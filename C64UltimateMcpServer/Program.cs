using System.Collections.Concurrent;
using C64UltimateMcpServer;
using C64UltimateClient;
using C64UltimateMcpServer.Core;
using C64UltimateMcpServer.Resources;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Extensions.Tasks;

ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions = new();

var builder = WebApplication.CreateBuilder(args);
var taskStore = new InMemoryMcpTaskStore();

var baseUrl = builder.Configuration["Ultimate:BaseUrl"] ?? "http://192.168.0.120";
Console.WriteLine($"[Config] Ultimate BaseUrl: {baseUrl}");

// Register UltimateService for DI
builder.Services.AddScoped<UltimateService>();
builder.Services.AddScoped<C64ResourceProvider>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<McpSessionNotifications>();

// Configure MCP Server AFTER setting up configuration
builder.Services
    .AddMcpServer(options =>
    {
        // Configure server implementation details with icons and website
        options.ServerInfo = new Implementation
        {
            Name = "C64 Ultimate MCP Server",
            Version = "1.0.0",
            Title = "MCP Server for Commodore 64 Ultimate",
            Description = "A comprehensive MCP server for the Commodore 64 Ultimate with 45+ tools for device control, file management, and machine operations",
            WebsiteUrl = "https://ultimate64.com/",
            Icons = [
                new Icon
                {
                    Source = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg",
                    MimeType = "image/svg+xml",
                    Sizes = ["any"],
                    Theme = "light"
                }
            ]
        };
        // Note: Custom JsonSerializerOptions are configured per-service (see UltimateService)
    })
    .WithHttpTransport(options => ConfigureHttpTransport(options, subscriptions))
    .WithTools<UltimateService>()
    .WithTasks(taskStore)
    .WithPrompts<C64UltimatePrompts>()
    .WithSubscribeToResourcesHandler((ctx, ct) =>
    {
        if (ctx.Server.SessionId is { } sessionId && ctx.Params?.Uri is { } uri)
        {
            subscriptions.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, byte>())
                .TryAdd(uri, 0);
        }
        return ValueTask.FromResult(new EmptyResult());
    })
    .WithUnsubscribeFromResourcesHandler((ctx, ct) =>
    {
        if (ctx.Server.SessionId is { } sessionId &&
            ctx.Params?.Uri is { } uri &&
            subscriptions.TryGetValue(sessionId, out var sessionSubscriptions))
        {
            sessionSubscriptions.TryRemove(uri, out _);
        }
        return ValueTask.FromResult(new EmptyResult());
    })
    .WithResources<C64ResourceProvider>();

var app = builder.Build();

// Map MCP endpoint
app.MapMcp();

// Keep a simple health check endpoint
app.MapGet("/health", () => "C64 Ultimate MCP Server is running");

app.Run();

static void ConfigureHttpTransport(
    HttpServerTransportOptions options,
    ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions)
{
    options.SessionMode = HttpServerSessionMode.StatefulForInitializeClients;

    // Session workers need a lifetime callback; ConfigureSessionOptions has no cleanup hook.
    #pragma warning disable MCPEXP002
    options.RunSessionHandler = async (httpContext, mcpServer, token) =>
    {
        if (mcpServer.SessionId == null)
        {
            await mcpServer.RunAsync(token);
            return;
        }

        var notifications = httpContext.RequestServices.GetRequiredService<McpSessionNotifications>();
        await notifications.RunAsync(mcpServer, subscriptions, token);
    };
    #pragma warning restore MCPEXP002
}

public partial class Program { }

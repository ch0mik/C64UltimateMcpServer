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

ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions = new();

var builder = WebApplication.CreateBuilder(args);
#pragma warning disable MCPEXP001
var taskStore = new InMemoryMcpTaskStore();
#pragma warning restore MCPEXP001

var baseUrl = builder.Configuration["Ultimate:BaseUrl"] ?? "http://192.168.0.120";
Console.WriteLine($"[Config] Ultimate BaseUrl: {baseUrl}");

// Register UltimateService for DI
builder.Services.AddScoped<UltimateService>();
builder.Services.AddScoped<C64ResourceProvider>();

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
        #pragma warning disable MCPEXP001
        options.TaskStore = taskStore;
        options.SendTaskStatusNotifications = true;
        #pragma warning restore MCPEXP001
        // Note: Custom JsonSerializerOptions are configured per-service (see UltimateService)
        // MCP SDK 1.0.0 supports user-defined serialization options
    })
    .WithHttpTransport(options =>
        {
            // Add a RunSessionHandler to remove all subscriptions for the session when it ends
            #pragma warning disable MCPEXP002
            options.RunSessionHandler = async (httpContext, mcpServer, token) =>
            {
                if (mcpServer.SessionId == null)
                {
                    // There is no sessionId if the serverOptions.Stateless is true
                    await mcpServer.RunAsync(token);
                    return;
                }
                try
                {
                    subscriptions[mcpServer.SessionId] = new ConcurrentDictionary<string, byte>();
                    // Start an instance of SubscriptionMessageSender for this session
                    using var subscriptionSender = new SubscriptionMessageSender(mcpServer, subscriptions[mcpServer.SessionId]);
                    await subscriptionSender.StartAsync(token);
                    // Start an instance of LoggingUpdateMessageSender for this session
                    using var loggingSender = new LoggingUpdateMessageSender(mcpServer);
                    await loggingSender.StartAsync(token);
                    await mcpServer.RunAsync(token);
                }
                finally
                {
                    // This code runs when the session ends
                    subscriptions.TryRemove(mcpServer.SessionId, out _);
                }
            };
            #pragma warning restore MCPEXP002
        })
    .WithTools<UltimateService>()
    .WithPrompts<C64UltimatePrompts>()
    .WithSubscribeToResourcesHandler(async (ctx, ct) =>
    {
        if (ctx.Server.SessionId is { } sessionId && ctx.Params?.Uri is { } uri)
        {
            subscriptions.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, byte>())
                .TryAdd(uri, 0);
        }
        return new EmptyResult();
    })
    .WithUnsubscribeFromResourcesHandler(async (ctx, ct) =>
    {
        if (ctx.Server.SessionId is { } sessionId &&
            ctx.Params?.Uri is { } uri &&
            subscriptions.TryGetValue(sessionId, out var sessionSubscriptions))
        {
            sessionSubscriptions.TryRemove(uri, out _);
        }
        return new EmptyResult();
    })
    .WithResources<C64ResourceProvider>();

var app = builder.Build();

// Map MCP endpoint
app.MapMcp();

// Keep a simple health check endpoint
app.MapGet("/health", () => "C64 Ultimate MCP Server is running");

app.Run();










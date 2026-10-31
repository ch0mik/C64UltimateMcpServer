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
using C64UltimateMcpServer.Features;
using ModelContextProtocol.Extensions.Apps;
using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;

ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions = new();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddValidatedApplicationOptions(builder.Configuration);
var taskStore = new SdkTaskStoreAdapter();

// Register UltimateService for DI
builder.Services.AddScoped(services => new UltimateService(services.GetRequiredService<ILogger<UltimateService>>(),
    services.GetRequiredService<ILogger<Ultimate64Client>>(), services.GetRequiredService<UltimateDeviceClientFactory>(),
    services.GetRequiredService<DownloadClientFactory>()));
builder.Services.AddScoped<C64ResourceProvider>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<McpSessionNotifications>();
builder.Services.AddSingleton<Func<HttpMessageHandler>>(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(services => new UltimateDeviceClientFactory(services.GetRequiredService<IOptions<UltimateClientOptions>>(),
    services.GetRequiredService<ILoggerFactory>(), services.GetRequiredService<Func<HttpMessageHandler>>()));
builder.Services.AddSingleton(services => new DownloadClientFactory(services.GetRequiredService<IOptions<DownloadOptions>>()));
builder.Services.AddSingleton<ProgramArtifactStore>();
builder.Services.AddSingleton<BoundedTaskStore>();
builder.Services.AddSingleton<DeviceResourceHub>();
builder.Services.AddHostedService(services => services.GetRequiredService<DeviceResourceHub>());
builder.Services.AddSingleton<ProgramWorkflow>();
builder.Services.AddSingleton<WorkflowTaskRunner>();
builder.Services.AddHostedService(services => services.GetRequiredService<WorkflowTaskRunner>());

// Configure MCP Server AFTER setting up configuration
builder.Services
    .AddMcpServer(options =>
    {
        options.Capabilities = new ServerCapabilities
        {
            Resources = new() { Subscribe = true, ListChanged = false },
            Tools = new() { ListChanged = false }, Prompts = new() { ListChanged = false }
        };
        // SDK 2.3 cannot compose attributed MRTR tools with Tasks; the workflow owns those rounds.
        #pragma warning disable MCPEXP002
        options.Filters.Request.CallToolWithAlternateFilters.Add(async (request, next, ct) =>
        {
            if (request.Params?.Name != "ultimate_program_workflow" ||
                request.Params.Meta?["io.modelcontextprotocol/protocolVersion"]?.GetValue<string>() != "2026-07-28" ||
                request.Params.Meta?["io.modelcontextprotocol/clientCapabilities"]?["extensions"]?["io.modelcontextprotocol/tasks"] is null)
                return await next(request, ct);
            var runner = request.Services!.GetRequiredService<WorkflowTaskRunner>();
            var created = await runner.LaunchAsync(new(request.Server.IsMrtrSupported, request.Server.ClientCapabilities), request.Params, ct);
            return ResultOrAlternate<CallToolResult>.FromAlternate(created, McpTasksJsonContext.Default.CreateTaskResult);
        });
        #pragma warning restore MCPEXP002
        // Note: Custom JsonSerializerOptions are configured per-service (see UltimateService)
    })
    .WithHttpTransport(options => ConfigureHttpTransport(options, subscriptions))
    .WithTools<UltimateService>()
    .WithTools<WorkflowTools>()
    .WithTools<AppTools>()
    .WithTasks(taskStore)
    .WithContentCache()
    .WithSubscriptionsListenHandler(FeatureProtocol.ListenAsync)
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
            ctx.Services!.GetRequiredService<DeviceResourceHub>().InterestsChanged();
        }
        return ValueTask.FromResult(new EmptyResult());
    })
    .WithResources<C64ResourceProvider>()
    .WithResources<FeatureResources>()
    .WithResources<AppTools>()
    // MCP Apps is experimental in SDK 2.3; isolated to the UI feature module.
    #pragma warning disable MCPEXP003
    .WithMcpApps();
    #pragma warning restore MCPEXP003

builder.Services.AddOptions<McpServerOptions>().Configure<IOptions<McpServerIdentityOptions>>((server, settings) =>
{
    var identity = settings.Value;
    server.ServerInfo = new Implementation
    {
        Name = identity.Name, Version = identity.Version, Title = identity.Title,
        Description = identity.Description, WebsiteUrl = identity.WebsiteUrl, Icons = identity.Icons
    };
});
builder.Services.AddOptions<HttpServerTransportOptions>().Configure<IOptions<McpTransportOptions>>((transport, settings) =>
{
    var value = settings.Value;
    transport.SessionMode = Enum.Parse<HttpServerSessionMode>(value.SessionMode);
    // Idle controls apply only to initialize-based sessions in stateful/hybrid modes.
    #pragma warning disable MCP9006
    transport.IdleTimeout = value.IdleTimeout;
    transport.MaxIdleSessionCount = value.MaxIdleSessionCount;
    #pragma warning restore MCP9006
});

var app = builder.Build();
taskStore.Initialize(app.Services.GetRequiredService<BoundedTaskStore>());
// Resolve the validated startup snapshot before serving any request. Changes require restart.
var oauth = app.Services.GetRequiredService<IOptions<McpOAuthOptions>>().Value;
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoint
var transportSettings = app.Services.GetRequiredService<IOptions<McpTransportOptions>>().Value;
var mcpEndpoint = app.MapMcp(transportSettings.EndpointPath);
if (oauth.Enabled) mcpEndpoint.RequireAuthorization(McpOAuthConfiguration.AccessPolicy);

// Keep a simple health check endpoint
app.MapGet(transportSettings.HealthPath, () => "C64 Ultimate MCP Server is running");

app.Run();

static void ConfigureHttpTransport(
    HttpServerTransportOptions options,
    ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions)
{

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

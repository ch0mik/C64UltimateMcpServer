using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Features;

internal static class FeatureProtocol
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static CallToolResult Result(object data, bool error = false) => new()
    {
        IsError = error, StructuredContent = JsonSerializer.SerializeToElement(data, Json),
        Content = [new TextContentBlock { Text = JsonSerializer.Serialize(data, Json) }]
    };

    internal static async ValueTask<EmptyResult> ListenAsync(RequestContext<SubscriptionsListenRequestParams> context, CancellationToken ct)
    {
        var hub = context.Services!.GetRequiredService<DeviceResourceHub>();
        var granted = context.Params!.Notifications.ResourceSubscriptions?.Where(DeviceResourceHub.IsLive).Distinct().ToArray() ?? [];
        using var listener = hub.Watch(() => granted);
        async Task Send(string method, JsonObject payload)
        {
            payload["_meta"] = new JsonObject { ["subscriptionId"] = JsonSerializer.SerializeToNode(context.JsonRpcRequest.Id, McpJsonUtilities.DefaultOptions) };
            await context.Server.SendMessageAsync(new JsonRpcNotification { Method = method, Params = payload }, ct);
        }
        await Send(NotificationMethods.SubscriptionsAcknowledgedNotification,
            new JsonObject { ["notifications"] = new JsonObject { ["resourceSubscriptions"] = JsonSerializer.SerializeToNode(granted) } });
        await foreach (var uri in listener.ReadAllAsync(ct))
            await Send(NotificationMethods.ResourceUpdatedNotification, new JsonObject { ["uri"] = uri });
        return new();
    }

    internal static IMcpServerBuilder WithContentCache(this IMcpServerBuilder builder) => builder.WithRequestFilters(filters =>
    {
        filters.AddListToolsFilter(next => async (ctx, ct) =>
        {
            var result = await next(ctx, ct);
            if (ctx.Server.ClientCapabilities?.Extensions?.ContainsKey("io.modelcontextprotocol/ui") != true)
                result.Tools = result.Tools.Where(t => t.Name != "ultimate_request_program_execution").ToList();
            return Private(result, IsModern(ctx.Server));
        });
        filters.AddListPromptsFilter(next => async (ctx, ct) => Private(await next(ctx, ct), IsModern(ctx.Server)));
        filters.AddListResourcesFilter(next => async (ctx, ct) => Private(await next(ctx, ct), IsModern(ctx.Server)));
        filters.AddListResourceTemplatesFilter(next => async (ctx, ct) => Private(await next(ctx, ct), IsModern(ctx.Server)));
        filters.AddReadResourceFilter(next => async (ctx, ct) =>
        {
            var result = await next(ctx, ct);
            if (!IsModern(ctx.Server)) return result;
            var uri = ctx.Params?.Uri ?? "";
            result.TimeToLive = TimeSpan.Zero;
            result.CacheScope = CacheScope.Private;
            if (uri.StartsWith("c64://artifacts/", StringComparison.Ordinal))
            {
                var artifact = ctx.Services!.GetRequiredService<ProgramArtifactStore>().Get(uri["c64://artifacts/".Length..]);
                result.TimeToLive = artifact.Info.ExpiresAt - ctx.Services!.GetRequiredService<TimeProvider>().GetUtcNow();
            }
            else if (uri.StartsWith("c64://", StringComparison.Ordinal) && !DeviceResourceHub.IsLive(uri))
            { result.TimeToLive = ctx.Services!.GetRequiredService<IOptions<ContentCacheOptions>>().Value.DocumentationTimeToLive; result.CacheScope = CacheScope.Public; }
            return result;
        });
    });

    private static bool IsModern(McpServer server) => string.CompareOrdinal(server.NegotiatedProtocolVersion, "2026-07-28") >= 0;
    private static T Private<T>(T result, bool modern) where T : ICacheableResult
    { if (modern) { result.TimeToLive = TimeSpan.Zero; result.CacheScope = CacheScope.Private; } return result; }
}

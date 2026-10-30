using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using C64UltimateMcpServer.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ModelContextProtocol.Server;
using Xunit;

namespace C64UltimateClient.Tests;

public class McpHttpIntegrationTests
{
    [Fact]
    public async Task LegacySubscriptions_SendOnlySubscribedUrisAndHonorUnsubscribe()
    {
        using var factory = new McpAppFactory();
        using var first = new RpcClient(factory.CreateClient(), false);
        using var second = new RpcClient(factory.CreateClient(), false);
        await first.ConnectAsync();
        await second.ConnectAsync();
        await first.CallAsync("logging/setLevel", new() { ["level"] = "emergency" });
        await first.CallAsync("resources/subscribe", new() { ["uri"] = "c64://basic/spec" });
        await second.CallAsync("resources/subscribe", new() { ["uri"] = "c64://memory/map" });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var firstUpdates = Channel.CreateUnbounded<JsonNode>();
        var secondUpdates = Channel.CreateUnbounded<JsonNode>();
        var firstPump = first.ReadResourceUpdatesAsync(firstUpdates.Writer, timeout.Token);
        var secondPump = second.ReadResourceUpdatesAsync(secondUpdates.Writer, timeout.Token);
        var firstUpdate = firstUpdates.Reader.ReadAsync(timeout.Token).AsTask();
        var secondUpdate = secondUpdates.Reader.ReadAsync(timeout.Token).AsTask();
        await AdvanceUntilAsync(factory, () => firstUpdate.IsCompleted && secondUpdate.IsCompleted, timeout.Token);
        Assert.Equal("c64://basic/spec", (await firstUpdate)["params"]!["uri"]!.GetValue<string>());
        Assert.Equal("c64://memory/map", (await secondUpdate)["params"]!["uri"]!.GetValue<string>());

        await first.CallAsync("resources/unsubscribe", new() { ["uri"] = "c64://basic/spec" });
        using var unsubscribed = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        // Drain any notifications already sent before unsubscribe; subsequent ticks must omit this URI.
        while (firstUpdates.Reader.TryRead(out _)) { }
        var noUpdate = firstUpdates.Reader.ReadAsync(unsubscribed.Token).AsTask();
        var nextUpdate = secondUpdates.Reader.ReadAsync(timeout.Token).AsTask();
        await AdvanceUntilAsync(factory, () => nextUpdate.IsCompleted, timeout.Token);
        await nextUpdate;
        Assert.False(noUpdate.IsCompletedSuccessfully);
        unsubscribed.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => noUpdate);
        await first.DeleteSessionAsync();
        await second.DeleteSessionAsync();
        await EventuallyAsync(() => factory.Notifications.ActiveSessions == 0);
        timeout.Cancel();
        await Task.WhenAll(firstPump, secondPump);
    }

    private static async Task AdvanceUntilAsync(McpAppFactory factory, Func<bool> condition, CancellationToken ct)
    {
        while (!condition())
        {
            factory.Clock.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(10, ct);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrimitivesAndResults_MatchSdk14Baseline(bool modern)
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), modern);
        var handshake = await rpc.ConnectAsync();
        Assert.Equal(modern ? null : "2025-11-25", handshake["protocolVersion"]?.GetValue<string>());
        Assert.Equal("C64 Ultimate MCP Server is running", await rpc.Http.GetStringAsync("/health"));

        var baseline = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "mcp-1.4-baseline.json")))!;
        foreach (var (method, field, key) in new[] { ("tools/list", "tools", "name"), ("prompts/list", "prompts", "name"), ("resources/list", "resources", "uri") })
        {
            var actual = await rpc.CallAsync(method);
            var expectedItems = baseline[field]![field]!.AsArray().ToDictionary(n => n![key]!.GetValue<string>());
            var actualItems = actual[field]!.AsArray().ToDictionary(n => n![key]!.GetValue<string>());
            Assert.Equal(expectedItems.Keys.Order(), actualItems.Keys.Order());
            foreach (var name in expectedItems.Keys)
            {
                // SDK 2 changes execution/caching metadata, but the callable input contract is stable.
                if (field == "tools") AssertJson(expectedItems[name]!["inputSchema"], actualItems[name]!["inputSchema"]);
            }
        }
        AssertJson(baseline["prompt"]!["messages"], (await rpc.CallAsync("prompts/get", new() { ["name"] = "basic-program" }))["messages"]);
        AssertJson(baseline["resource"]!["contents"], (await rpc.CallAsync("resources/read", new() { ["uri"] = "c64://basic/spec" }))["contents"]);

        foreach (var (key, name, args) in new[]
        {
            ("basic", "ultimate_generate_basic_prg", new JsonObject { ["basicSource"] = "10 PRINT \"HELLO\"" }),
            ("assembly", "ultimate_generate_assembly_prg", new JsonObject { ["assemblySource"] = ".org $C000\nRTS" }),
            ("error", "ultimate_generate_basic_prg", new JsonObject { ["basicSource"] = "NOT BASIC" }),
            ("text", "ultimate_set_connection", new JsonObject { ["hostname"] = "localhost" })
        })
        {
            var actual = await rpc.CallAsync("tools/call", new() { ["name"] = name, ["arguments"] = args });
            AssertJson(baseline[key]!["content"], actual["content"]);
            AssertJson(baseline[key]!["structuredContent"], actual["structuredContent"]);
            Assert.Equal(baseline[key]!["isError"]?.GetValue<bool>() ?? false, actual["isError"]?.GetValue<bool>() ?? false);
            Assert.Null(actual["taskId"]);
        }
        await rpc.DeleteSessionAsync();
        await EventuallyAsync(() => factory.Notifications.ActiveSessions == 0);
    }

    [Fact]
    public async Task HttpTransport_UsesIndependentLegacySessionsAndNoModernSession()
    {
        using var factory = new McpAppFactory();
        using var first = new RpcClient(factory.CreateClient(), false);
        using var second = new RpcClient(factory.CreateClient(), false);
        await first.ConnectAsync();
        await second.ConnectAsync();
        Assert.NotEqual(first.SessionId, second.SessionId);
        Assert.Equal(2, factory.Notifications.ActiveSessions);
        await first.DeleteSessionAsync();
        await EventuallyAsync(() => factory.Notifications.ActiveSessions == 1);
        await second.CallAsync("tools/list");
        await second.DeleteSessionAsync();
        await EventuallyAsync(() => factory.Notifications.ActiveSessions == 0);

        using var modern = new RpcClient(factory.CreateClient(), true);
        await modern.ConnectAsync();
        await modern.CallAsync("tools/list");
        Assert.Null(modern.SessionId);
        Assert.Equal(0, factory.Notifications.ActiveSessions);
        using var get = new HttpRequestMessage(HttpMethod.Get, "/");
        get.Headers.Add("MCP-Protocol-Version", "2026-07-28");
        using var response = await modern.Http.SendAsync(get);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await modern.Http.GetAsync("/sse")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await modern.Http.PostAsync("/message", new StringContent("{}"))).StatusCode);
    }

    [Theory]
    [InlineData("10 PRINT \"HELLO\"", false)]
    [InlineData("NOT BASIC", true)]
    public async Task Tasks_OptInAndPollingPreserveToolResults(string source, bool isError)
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true);
        var discovery = await rpc.ConnectAsync();
        Assert.Contains("io.modelcontextprotocol/tasks", discovery.ToJsonString());
        var args = new JsonObject { ["name"] = "ultimate_generate_basic_prg", ["arguments"] = new JsonObject { ["basicSource"] = source } };
        var ordinary = await rpc.CallAsync("tools/call", (JsonObject)args.DeepClone());
        Assert.Null(ordinary["taskId"]);
        var created = await rpc.CallAsync("tools/call", args, tasks: true);
        Assert.Equal("task", created["resultType"]?.GetValue<string>());
        var taskId = created["taskId"]!.GetValue<string>();
        JsonNode state = created;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (state["status"]!.GetValue<string>() == "working")
        {
            await Task.Delay(10, timeout.Token);
            state = await rpc.CallAsync("tasks/get", new() { ["taskId"] = taskId }, tasks: true);
        }
        Assert.Equal("completed", state["status"]!.GetValue<string>());
        var result = state["result"]!;
        Assert.Equal(isError, result["isError"]?.GetValue<bool>() ?? false);
        AssertJson(ordinary["content"], result["content"]);
        AssertJson(ordinary["structuredContent"], result["structuredContent"]);
        Assert.Equal(0, factory.Notifications.ActiveSessions);
    }

    [Fact]
    public async Task Tasks_CancelPropagatesToRunningTool()
    {
        using var factory = new McpAppFactory(includeSlowTool: true);
        using var rpc = new RpcClient(factory.CreateClient(), true);
        await rpc.ConnectAsync();
        var created = await rpc.CallAsync("tools/call", new() { ["name"] = "test_wait", ["arguments"] = new JsonObject() }, tasks: true);
        await factory.Work.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var taskId = created["taskId"]!.GetValue<string>();
        await rpc.CallAsync("tasks/cancel", new() { ["taskId"] = taskId }, tasks: true);
        await factory.Work.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var state = await rpc.CallAsync("tasks/get", new() { ["taskId"] = taskId }, tasks: true);
        Assert.Equal("cancelled", state["status"]!.GetValue<string>());
    }

    internal static async Task EventuallyAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    private static void AssertJson(JsonNode? expected, JsonNode? actual) =>
        Assert.True(JsonNode.DeepEquals(expected, actual), $"Expected {expected}; actual {actual}");
}

internal sealed class McpAppFactory(bool includeSlowTool = false) : WebApplicationFactory<Program>
{
    internal FakeTimeProvider Clock { get; } = new();
    internal TestWork Work { get; } = new();
    internal McpSessionNotifications Notifications => Services.GetRequiredService<McpSessionNotifications>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            if (includeSlowTool)
            {
                services.AddSingleton(Work);
                services.AddMcpServer().WithTools([McpServerTool.Create(Work.Wait, new() { Name = "test_wait" })]);
            }
        });
    }
}

public sealed class TestWork
{
    internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [McpServerTool(Name = "test_wait")]
    public async Task<string> Wait(CancellationToken cancellationToken)
    {
        Started.TrySetResult();
        try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
        catch (OperationCanceledException) { Cancelled.TrySetResult(); throw; }
        return "finished";
    }
}

internal sealed class RpcClient(HttpClient http, bool modern) : IDisposable
{
    internal HttpClient Http => http;
    internal string? SessionId { get; private set; }
    private int _id;

    internal async Task<JsonNode> ConnectAsync()
    {
        if (modern) return await CallAsync("server/discover");
        var result = await CallAsync("initialize", new()
        {
            ["protocolVersion"] = "2025-11-25", ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject { ["name"] = "integration", ["version"] = "1" }
        });
        using var initialized = CreateRequest("notifications/initialized", new JsonObject(), notification: true);
        using var response = await http.SendAsync(initialized);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return result;
    }

    internal async Task<JsonNode> CallAsync(string method, JsonObject? parameters = null, bool tasks = false)
    {
        using var request = CreateRequest(method, parameters ?? new(), tasks);
        using var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{method}: {response.StatusCode} {body}");
        if (response.Headers.TryGetValues("Mcp-Session-Id", out var values)) SessionId = values.Single();
        if (modern) Assert.False(response.Headers.Contains("Mcp-Session-Id"));
        var json = response.Content.Headers.ContentType?.MediaType == "text/event-stream"
            ? body.Split('\n').Where(l => l.StartsWith("data:")).Select(l => JsonNode.Parse(l[5..].Trim())!).Single(n => n["id"] is not null)
            : JsonNode.Parse(body)!;
        Assert.Null(json["error"]);
        return json["result"]!;
    }

    private HttpRequestMessage CreateRequest(string method, JsonObject parameters, bool tasks = false, bool notification = false)
    {
        if (modern)
        {
            parameters["_meta"] = new JsonObject
            {
                ["io.modelcontextprotocol/protocolVersion"] = "2026-07-28",
                ["io.modelcontextprotocol/clientInfo"] = new JsonObject { ["name"] = "integration", ["version"] = "1" },
                ["io.modelcontextprotocol/clientCapabilities"] = tasks
                    ? new JsonObject { ["extensions"] = new JsonObject { ["io.modelcontextprotocol/tasks"] = new JsonObject() } }
                    : new JsonObject()
            };
        }
        var body = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = parameters };
        if (!notification) body["id"] = Interlocked.Increment(ref _id);
        var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        request.Headers.Add("Accept", "application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", modern ? "2026-07-28" : "2025-11-25");
        if (SessionId is not null) request.Headers.Add("Mcp-Session-Id", SessionId);
        if (modern)
        {
            request.Headers.Add("Mcp-Method", method);
            var name = parameters["name"] ?? parameters["uri"] ?? parameters["taskId"];
            if (name is not null) request.Headers.Add("Mcp-Name", name.GetValue<string>());
        }
        return request;
    }

    internal async Task DeleteSessionAsync()
    {
        if (SessionId is null) return;
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/");
        request.Headers.Add("Mcp-Session-Id", SessionId);
        request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        using var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SessionId = null;
    }

    internal async Task ReadResourceUpdatesAsync(ChannelWriter<JsonNode> writer, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("Accept", "text/event-stream");
            request.Headers.Add("Mcp-Session-Id", SessionId);
            request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) Assert.Fail(await response.Content.ReadAsStringAsync(ct));
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (!line.StartsWith("data:")) continue;
                var notification = JsonNode.Parse(line[5..].Trim())!;
                if (notification["method"]?.GetValue<string>() == "notifications/resources/updated") await writer.WriteAsync(notification, ct);
            }
            writer.TryComplete();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { writer.TryComplete(); }
        catch (Exception ex) { writer.TryComplete(ex); throw; }
    }

    public void Dispose() => http.Dispose();
}

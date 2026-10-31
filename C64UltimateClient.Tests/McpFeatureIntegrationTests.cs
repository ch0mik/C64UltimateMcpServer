using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using C64UltimateMcpServer.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Text.Json;
using System.Collections.Generic;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace C64UltimateClient.Tests;

public class McpFeatureIntegrationTests
{
    [Theory]
    [InlineData(" ", "")]
    [InlineData("basic", " ")]
    public async Task Mrtr_ReplacesBlankInitialArgumentsWithElicitedValues(string language, string source)
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var args = new JsonObject { ["language"] = language, ["source"] = source };
        var result = await rpc.CallAsync("tools/call", Call(args));
        for (var round = 0; result["resultType"]?.GetValue<string>() == "input_required" && round < 3; round++)
        {
            var field = result["inputRequests"]!["parameters"] is not null ? "parameters" : "source";
            var content = field == "parameters" ? new JsonObject { ["language"] = "basic" } : new JsonObject { ["source"] = "10 END" };
            result = await rpc.CallAsync("tools/call", Call(args, result["requestState"], field, Accept(content)));
        }
        Assert.False(result["isError"]?.GetValue<bool>() ?? false);
        Assert.NotNull(result["structuredContent"]!["artifact"]);
    }
    [Fact]
    public async Task LegacyClient_ResolvesMrtrUsingSdkCompatibilityResolver()
    {
        using var factory = new McpAppFactory();
        using var http = factory.CreateClient();
        await using var transport = new HttpClientTransport(new() { Endpoint = http.BaseAddress!, TransportMode = HttpTransportMode.StreamableHttp }, http);
        var prompts = 0;
        await using var client = await McpClient.CreateAsync(transport, new McpClientOptions
        {
            ProtocolVersion = "2025-11-25",
            Capabilities = new() { Elicitation = new() { Form = new() } },
            Handlers = new() { ElicitationHandler = (request, ct) =>
            {
                Interlocked.Increment(ref prompts);
                Assert.Contains("SHA-256", request!.Message);
                return ValueTask.FromResult(new ElicitResult { Action = "accept", Content = new Dictionary<string, JsonElement>
                { ["confirm"] = JsonSerializer.SerializeToElement(true) } });
            } }
        });
        var result = await client.CallToolAsync("ultimate_program_workflow", new Dictionary<string, object?>
        { ["language"] = "basic", ["source"] = "10 END", ["execute"] = true });
        Assert.False(result.IsError ?? false);
        Assert.True(result.StructuredContent!.Value.GetProperty("executed").GetBoolean());
        Assert.Equal(1, prompts);
        Assert.Equal(1, factory.Device.Executions);
    }

    [Fact]
    public async Task ConcurrentConfirmation_ExecutesAtMostOnce()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var args = Args(execute: true);
        var pending = await rpc.CallAsync("tools/call", Call(args));
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => rpc.CallAsync("tools/call",
            Call(args, pending["requestState"], "confirm", Accept(new() { ["confirm"] = true })))));
        Assert.Single(results, r => r["isError"]?.GetValue<bool>() == true);
        Assert.Equal(1, factory.Device.Executions);
    }

    [Fact]
    public async Task Tasks_CancelWhileWaitingForInputDoesNotRunDevice()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var task = await rpc.CallAsync("tools/call", Call(Args(execute: true)), tasks: true);
        var id = task["taskId"]!.GetValue<string>();
        await PollAsync(rpc, id, "input_required");
        await rpc.CallAsync("tasks/cancel", new() { ["taskId"] = id }, tasks: true);
        await PollAsync(rpc, id, "cancelled");
        Assert.Equal(0, factory.Device.Executions);
    }
    [Fact]
    public async Task ModernListen_AcknowledgesSupportedSubsetAndStreamsOnlyContentChanges()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true);
        await rpc.ConnectAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request = rpc.CreateRequest("subscriptions/listen", new()
        { ["notifications"] = new JsonObject { ["toolsListChanged"] = true, ["resourceSubscriptions"] = new JsonArray(DeviceResourceHub.StatusUri, "c64://basic/spec", "unsupported://x") } });
        using var response = await rpc.Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
        var ack = await ReadEventAsync(reader, timeout.Token);
        Assert.Equal("notifications/subscriptions/acknowledged", ack["method"]!.GetValue<string>());
        Assert.Equal(DeviceResourceHub.StatusUri, Assert.Single(ack["params"]!["notifications"]!["resourceSubscriptions"]!.AsArray())!.GetValue<string>());
        Assert.Null(ack["params"]!["notifications"]!["toolsListChanged"]);
        var hub = factory.Services.GetRequiredService<DeviceResourceHub>();
        await rpc.CallAsync("resources/read", new() { ["uri"] = DeviceResourceHub.StatusUri });
        var update = await ReadEventAsync(reader, timeout.Token);
        Assert.Equal(ack["params"]!["_meta"]!["subscriptionId"]!.ToJsonString(), update["params"]!["_meta"]!["subscriptionId"]!.ToJsonString());
        Assert.Equal(DeviceResourceHub.StatusUri, update["params"]!["uri"]!.GetValue<string>());
        // An unchanged read must not enqueue an event; the next event should describe the offline transition.
        await rpc.CallAsync("resources/read", new() { ["uri"] = DeviceResourceHub.StatusUri });
        factory.Device.Offline = true;
        var offline = await rpc.CallAsync("resources/read", new() { ["uri"] = DeviceResourceHub.StatusUri });
        Assert.Contains("\"available\":false", offline["contents"]![0]!["text"]!.GetValue<string>());
        await ReadEventAsync(reader, timeout.Token);
        using var noChange = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var pending = ReadEventAsync(reader, noChange.Token);
        noChange.CancelAfter(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        timeout.Cancel();
        response.Dispose();
        await McpHttpIntegrationTests.EventuallyAsync(() => hub.ListenerCount == 0);
    }

    private static async Task<JsonNode> ReadEventAsync(StreamReader reader, CancellationToken ct)
    {
        while (await reader.ReadLineAsync(ct) is { } line)
            if (line.StartsWith("data:")) return JsonNode.Parse(line[5..].Trim())!;
        throw new InvalidOperationException("Stream ended.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectProgress_PreservesNumericAndStringTokens(bool numeric)
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true);
        await rpc.ConnectAsync();
        var parameters = Call(Args());
        JsonNode token = numeric ? JsonValue.Create(42)! : JsonValue.Create("build-42")!;
        parameters["_meta"] = new JsonObject { ["progressToken"] = token.DeepClone() };
        await rpc.CallAsync("tools/call", parameters);
        var notifications = rpc.Notifications.Where(n => n["method"]!.GetValue<string>() == "notifications/progress").ToArray();
        Assert.NotEmpty(notifications);
        Assert.All(notifications, n => Assert.True(JsonNode.DeepEquals(token, n["params"]!["progressToken"])));
        var progress = notifications.Select(n => n["params"]!["progress"]!.GetValue<float>()).ToArray();
        Assert.Equal(progress.Order(), progress);
    }
    private static JsonObject FormCapabilities() => new() { ["elicitation"] = new JsonObject { ["form"] = new JsonObject() } };
    private static JsonObject Args(string? source = "10 PRINT \"HELLO\"", bool execute = false) => new()
    { ["language"] = "basic", ["source"] = source, ["execute"] = execute };
    private static JsonObject Call(JsonObject arguments, JsonNode? state = null, string? input = null, JsonNode? answer = null) => new()
    {
        ["name"] = "ultimate_program_workflow", ["arguments"] = arguments.DeepClone(),
        ["requestState"] = state?.DeepClone(), ["inputResponses"] = input is null ? null : new JsonObject { [input] = answer?.DeepClone() }
    };
    private static JsonObject Accept(JsonObject content) => new() { ["action"] = "accept", ["content"] = content };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompileArtifactsAndCache_WorkOnBothProtocolPaths(bool modern)
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), modern);
        await rpc.ConnectAsync();
        var result = await rpc.CallAsync("tools/call", Call(Args()));
        Assert.False(result["isError"]!.GetValue<bool>());
        var artifact = result["structuredContent"]!["artifact"]!;
        var read = await rpc.CallAsync("resources/read", new() { ["uri"] = artifact["uri"]!.GetValue<string>() });
        var blob = read["contents"]![0]!["blob"]!.GetValue<string>();
        Assert.Matches("^[A-Za-z0-9+/=]*$", blob);
        var bytes = Convert.FromBase64String(blob);
        Assert.Equal(artifact["sizeBytes"]!.GetValue<int>(), bytes.Length);
        Assert.Equal(1, bytes[0]); Assert.Equal(8, bytes[1]);
        var doc = await rpc.CallAsync("resources/read", new() { ["uri"] = "c64://basic/spec" });
        var list = await rpc.CallAsync("tools/list");
        if (modern)
        {
            Assert.Equal(3600000, doc["ttlMs"]!.GetValue<int>());
            Assert.Equal("public", doc["cacheScope"]!.GetValue<string>());
            Assert.Equal("private", read["cacheScope"]!.GetValue<string>());
            Assert.InRange(read["ttlMs"]!.GetValue<double>(), 1, 3600000);
            Assert.Equal("private", list["cacheScope"]!.GetValue<string>());
            Assert.Equal(0, list["ttlMs"]!.GetValue<int>());
        }
        else { Assert.Null(doc["ttlMs"]); Assert.Null(read["cacheScope"]); }
        await rpc.DeleteSessionAsync();
    }

    [Fact]
    public async Task Mrtr_CollectsParametersAndSourceWithoutReadingClientPaths()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var args = new JsonObject();
        var parameters = await rpc.CallAsync("tools/call", Call(args));
        Assert.Equal("input_required", parameters["resultType"]!.GetValue<string>());
        var source = await rpc.CallAsync("tools/call", Call(args, parameters["requestState"], "parameters", Accept(new() { ["language"] = "basic" })));
        Assert.NotNull(source["inputRequests"]!["source"]);
        var compiled = await rpc.CallAsync("tools/call", Call(args, source["requestState"], "source", Accept(new() { ["source"] = "10 END" })));
        Assert.NotNull(compiled["structuredContent"]!["artifact"]);
        Assert.Equal(0, factory.Device.Executions);
        var replay = await rpc.CallAsync("tools/call", Call(args, source["requestState"], "source", Accept(new() { ["source"] = "10 END" })));
        Assert.True(replay["isError"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Mrtr_ConfirmationIsBoundToArgumentsAndConsumedOnlyOnce()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var args = Args(execute: true);
        var confirmation = await rpc.CallAsync("tools/call", Call(args));
        Assert.Contains("SHA-256", confirmation["inputRequests"]!["confirm"]!["params"]!["message"]!.GetValue<string>());
        Assert.Equal(0, factory.Device.Executions);
        var changed = Args("10 PRINT \"CHANGED\"", true);
        var rejected = await rpc.CallAsync("tools/call", Call(changed, confirmation["requestState"], "confirm", Accept(new() { ["confirm"] = true })));
        Assert.True(rejected["isError"]!.GetValue<bool>());
        var executed = await rpc.CallAsync("tools/call", Call(args, confirmation["requestState"], "confirm", Accept(new() { ["confirm"] = true })));
        Assert.True(executed["structuredContent"]!["executed"]!.GetValue<bool>());
        var replay = await rpc.CallAsync("tools/call", Call(args, confirmation["requestState"], "confirm", Accept(new() { ["confirm"] = true })));
        Assert.True(replay["isError"]!.GetValue<bool>());
        Assert.Equal(1, factory.Device.Executions);
    }

    [Fact]
    public async Task Mrtr_DeclineExpiredArtifactAndMissingCapabilitiesNeverExecute()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var args = Args(execute: true);
        var confirmation = await rpc.CallAsync("tools/call", Call(args));
        var declined = await rpc.CallAsync("tools/call", Call(args, confirmation["requestState"], "confirm", new JsonObject { ["action"] = "decline" }));
        Assert.False(declined["structuredContent"]!["executed"]!.GetValue<bool>());
        confirmation = await rpc.CallAsync("tools/call", Call(args));
        factory.Clock.Advance(TimeSpan.FromHours(1));
        var expired = await rpc.CallAsync("tools/call", Call(args, confirmation["requestState"], "confirm", Accept(new() { ["confirm"] = true })));
        Assert.True(expired["isError"]!.GetValue<bool>());
        rpc.Capabilities = new();
        var missing = await rpc.CallAsync("tools/call", Call(new()));
        Assert.Equal("MissingCapability", missing["structuredContent"]!["errorType"]!.GetValue<string>());
        Assert.Equal(0, factory.Device.Executions);
    }

    [Fact]
    public async Task Mrtr_RootsAndSamplingProduceArtifactAndMalformedSourceReturnsDiagnostics()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        rpc.Capabilities["roots"] = new JsonObject(); rpc.Capabilities["sampling"] = new JsonObject();
        await rpc.ConnectAsync();
        var args = new JsonObject { ["language"] = "basic", ["generationRequest"] = "Hello", ["selectProject"] = true };
        var roots = await rpc.CallAsync("tools/call", Call(args));
        var sampled = await rpc.CallAsync("tools/call", Call(args, roots["requestState"], "roots", new JsonObject
        { ["roots"] = new JsonArray(new JsonObject { ["uri"] = "file:///only-on-client/project", ["name"] = "project" }) }));
        Assert.NotNull(sampled["inputRequests"]!["sampling"]);
        var done = await rpc.CallAsync("tools/call", Call(args, sampled["requestState"], "sampling", new JsonObject
        { ["role"] = "assistant", ["model"] = "test", ["content"] = new JsonObject { ["type"] = "text", ["text"] = "10 END" } }));
        Assert.NotNull(done["structuredContent"]!["artifact"]);
        var error = await rpc.CallAsync("tools/call", Call(Args("NOT BASIC")));
        Assert.Equal("CompilationError", error["structuredContent"]!["errorType"]!.GetValue<string>());
    }

    [Fact]
    public async Task WorkflowTasks_ExposeInputRequiredAndResumeAfterUpdate()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var task = await rpc.CallAsync("tools/call", Call(Args(execute: true)), tasks: true);
        var id = task["taskId"]!.GetValue<string>();
        var waiting = await PollAsync(rpc, id, "input_required");
        Assert.NotNull(waiting["inputRequests"]!["confirm"]);
        Assert.NotNull(waiting["statusMessage"]);
        await rpc.CallAsync("tasks/update", new() { ["taskId"] = id, ["inputResponses"] = new JsonObject { ["confirm"] = Accept(new() { ["confirm"] = true }) } }, tasks: true);
        var completed = await PollAsync(rpc, id, "completed");
        Assert.True(completed["result"]!["structuredContent"]!["executed"]!.GetValue<bool>());
        Assert.Equal(1, factory.Device.Executions);
    }

    [Fact]
    public async Task WorkflowTaskCancellation_InterruptsDeviceHttpAndStopsFurtherWork()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = FormCapabilities() };
        await rpc.ConnectAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Device.Override = async (request, ct) =>
        {
            Assert.Equal("/v1/runners:run_prg", request.RequestUri!.AbsolutePath);
            Interlocked.Increment(ref factory.Device.Executions);
            started.SetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            throw new InvalidOperationException();
        };
        var task = await rpc.CallAsync("tools/call", Call(Args(execute: true)), tasks: true);
        var id = task["taskId"]!.GetValue<string>();
        await PollAsync(rpc, id, "input_required");
        await rpc.CallAsync("tasks/update", new() { ["taskId"] = id, ["inputResponses"] = new JsonObject { ["confirm"] = Accept(new() { ["confirm"] = true }) } }, tasks: true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await rpc.CallAsync("tasks/cancel", new() { ["taskId"] = id }, tasks: true);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await PollAsync(rpc, id, "cancelled");
        Assert.Equal(1, factory.Device.Executions);
    }

    [Fact]
    public async Task AppsMetadataResourcesAndFallback_ExposeLocalUiAndSafePreview()
    {
        using var factory = new McpAppFactory();
        using var rpc = new RpcClient(factory.CreateClient(), true);
        await rpc.ConnectAsync();
        var dashboard = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_dashboard", ["arguments"] = new JsonObject() });
        Assert.True(dashboard["structuredContent"]!["status"]!["available"]!.GetValue<bool>());
        Assert.NotEmpty(dashboard["content"]!.AsArray());
        var tools = await rpc.CallAsync("tools/list");
        var previewTool = tools["tools"]!.AsArray().Single(t => t!["name"]!.GetValue<string>() == "ultimate_program_preview")!;
        Assert.Equal("ui://c64/program-preview.html", previewTool["_meta"]!["ui"]!["resourceUri"]!.GetValue<string>());
        var ui = await rpc.CallAsync("resources/read", new() { ["uri"] = "ui://c64/program-preview.html" });
        var contents = ui["contents"]![0]!;
        Assert.Equal("text/html;profile=mcp-app", contents["mimeType"]!.GetValue<string>());
        Assert.Contains("Request execution", contents["text"]!.GetValue<string>());
        Assert.Empty(contents["_meta"]!["ui"]!["csp"]!["connectDomains"]!.AsArray());
        var info = factory.Services.GetRequiredService<ProgramArtifactStore>().Add(Enumerable.Range(0, 400).Select(i => (byte)i).ToArray());
        var preview = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_program_preview", ["arguments"] = new JsonObject { ["artifactId"] = info.Id } });
        Assert.Equal(512, preview["structuredContent"]!["hex"]!.GetValue<string>().Length);
        factory.Clock.Advance(TimeSpan.FromHours(1));
        var expired = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_program_preview", ["arguments"] = new JsonObject { ["artifactId"] = info.Id } });
        Assert.True(expired["isError"]!.GetValue<bool>());
        Assert.Equal(0, factory.Device.Executions);
    }

    internal static async Task<JsonNode> PollAsync(RpcClient rpc, string id, string status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var state = await rpc.CallAsync("tasks/get", new() { ["taskId"] = id }, tasks: true);
            var actual = state["status"]!.GetValue<string>();
            if (actual == status) return state;
            Assert.False(actual is "failed" or "cancelled" or "completed", state.ToJsonString());
            await Task.Delay(10, timeout.Token);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using C64UltimateClient;
using C64UltimateMcpServer.Configuration;
using C64UltimateMcpServer.Core;
using C64UltimateMcpServer.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ModelContextProtocol;
using Xunit;

namespace C64UltimateClient.Tests;

[CollectionDefinition("Environment options", DisableParallelization = true)]
public class EnvironmentOptionsCollection { }

[Collection("Environment options")]
public class ApplicationOptionsTests
{
    [Theory]
    [InlineData("Ultimate:BaseUrl", "ftp://device.example")]
    [InlineData("Ultimate:BaseUrl", "http://device.example/v1")]
    [InlineData("Ultimate:BaseUrl", "http://user:secret@device.example")]
    [InlineData("Ultimate:BaseUrl", "http://device.example?password=secret")]
    [InlineData("Ultimate:RequestTimeout", "00:00:00.099")]
    [InlineData("Ultimate:RequestTimeout", "00:10:00.001")]
    [InlineData("Downloads:RequestTimeout", "-00:00:01")]
    [InlineData("Mcp:Artifacts:MaxEntries", "0")]
    [InlineData("Mcp:Artifacts:MaxEntries", "10001")]
    [InlineData("Mcp:Artifacts:MaxBytes", "536870913")]
    [InlineData("Mcp:Artifacts:MaxArtifactBytes", "16777217")]
    [InlineData("Mcp:Artifacts:Lifetime", "1.00:00:01")]
    [InlineData("Mcp:Tasks:Capacity", "0")]
    [InlineData("Mcp:Tasks:TerminalRetention", "00:00:00")]
    [InlineData("Mcp:Tasks:PollInterval", "00:00:00.009")]
    [InlineData("Mcp:Workflow:Capacity", "10001")]
    [InlineData("Mcp:Workflow:ContinuationLifetime", "01:00:01")]
    [InlineData("Mcp:Workflow:MaxRounds", "65")]
    [InlineData("Mcp:Workflow:MaxSourceBytes", "0")]
    [InlineData("Mcp:Workflow:MaxGenerationRequestBytes", "1048577")]
    [InlineData("Mcp:Workflow:SamplingMaxTokens", "32769")]
    [InlineData("Mcp:Workflow:MaxRootHints", "1001")]
    [InlineData("Mcp:LiveResources:PollInterval", "00:00:00.099")]
    [InlineData("Mcp:LiveResources:ListenerQueueCapacity", "1")]
    [InlineData("Mcp:Cache:DocumentationTimeToLive", "-00:00:01")]
    [InlineData("Mcp:Apps:PreviewBytes", "257")]
    [InlineData("Mcp:Notifications:LegacyLoggingInterval", "00:00:00")]
    [InlineData("Mcp:Notifications:CleanupTimeout", "00:00:31")]
    [InlineData("Host:ShutdownTimeout", "00:00:00")]
    [InlineData("Mcp:Transport:EndpointPath", "/mcp/{name}")]
    [InlineData("Mcp:Transport:EndpointPath", "/a/../mcp")]
    [InlineData("Mcp:Transport:EndpointPath", "/mcp%2fhidden")]
    [InlineData("Mcp:Transport:HealthPath", "/")]
    [InlineData("Mcp:Transport:HealthPath", "/sse")]
    [InlineData("Mcp:Transport:HealthPath", "/.well-known/oauth-protected-resource")]
    [InlineData("Mcp:Transport:SessionMode", "2")]
    [InlineData("Mcp:Transport:IdleTimeout", "00:00:00")]
    [InlineData("Mcp:Transport:MaxIdleSessionCount", "100001")]
    [InlineData("Mcp:Server:Name", " ")]
    [InlineData("Mcp:Server:WebsiteUrl", "http://example.com")]
    [InlineData("Mcp:Cache:Typo", "secret-config-value")]
    [InlineData("Mcp:Typo:Enabled", "true")]
    [InlineData("Ultimate:RequestTimeout", "secret-invalid-value")]
    [InlineData("Ultimate:NetworkPassword", "secret\r\ninjected")]
    [InlineData("Mcp:OAuth", "true")]
    [InlineData("Ultimate", "http://secret-device.example")]
    [InlineData("Mcp:Artifacts", "secret-bad-section")]
    [InlineData("Mcp:OAuth:RequiredScopes", "secret-bad-array")]
    [InlineData("Mcp:Server:Icons", "secret-bad-array")]
    [InlineData("Mcp:Tasks:Capacity", "999999999999999999999999999999")]
    [InlineData("Mcp:Artifacts:MaxBytes", "9223372036854775808")]
    [InlineData("Ultimate:RequestTimeout", "999999999999999999999999:00:00")]
    public async Task InvalidConfiguration_FailsAtStartupWithoutEchoingValues(string key, string value)
    {
        var error = await Record.ExceptionAsync(async () =>
        {
            using var host = Host(new() { [key] = value });
            await host.StartAsync();
        });
        Assert.NotNull(error);
        var failures = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray() : [error];
        Assert.All(failures, failure => Assert.IsType<OptionsValidationException>(failure));
        if (value.Length > 16 || value.Contains("secret", StringComparison.Ordinal)) Assert.DoesNotContain(value, error.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EveryDocumentedNumericAndDurationLimit_AcceptsItsBoundary(bool maximum)
    {
        var settings = BoundarySettings(maximum);
        using var host = Host(settings);
        await host.StartAsync();
        // This builds only validated configuration; no large stores or device requests are allocated.
        Assert.Equal(maximum ? 536870912 : 2, host.Services.GetRequiredService<IOptions<ProgramArtifactOptions>>().Value.MaxBytes);
        await host.StopAsync();
    }

    public static IEnumerable<object[]> ValuesOutsideEveryDocumentedLimit()
    {
        foreach (var (key, minimum, maximum) in IntegerLimits())
        {
            yield return [key, (minimum - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)];
            yield return [key, (maximum + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)];
        }
        foreach (var (key, minimum, maximum) in DurationLimits())
        {
            yield return [key, minimum.Subtract(TimeSpan.FromTicks(1)).ToString("c")];
            yield return [key, maximum.Add(TimeSpan.FromTicks(1)).ToString("c")];
        }
    }

    [Theory]
    [MemberData(nameof(ValuesOutsideEveryDocumentedLimit))]
    public async Task EveryDocumentedLimit_RejectsImmediatelyOutsideBoundary(string key, string value)
    {
        var settings = BoundarySettings(true);
        settings[key] = value;
        var error = await Record.ExceptionAsync(async () =>
        {
            using var host = Host(settings);
            await host.StartAsync();
        });
        Assert.NotNull(error);
        var failures = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray() : [error];
        Assert.All(failures, failure => Assert.IsType<OptionsValidationException>(failure));
    }

    [Theory]
    [InlineData("MimeType", "image/<invalid>")]
    [InlineData("MimeType", "image/*")]
    [InlineData("MimeType", "image/png; injected=secret")]
    [InlineData("MimeType", "image/png\r\ninjected")]
    [InlineData("Sizes:0", "+16x+16")]
    [InlineData("Sizes:0", "16x 16")]
    [InlineData("Sizes:0", "99999999999999999999999x16")]
    [InlineData("Source", "https://icons.example/secret\r\nimage.svg")]
    public async Task MalformedIconMetadata_IsRejected(string key, string value)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Mcp:Server:Icons:0:Source"] = "https://icons.example/icon.svg",
            ["Mcp:Server:Icons:0:MimeType"] = "image/svg+xml",
            ["Mcp:Server:Icons:0:Sizes:0"] = "16x16"
        };
        settings["Mcp:Server:Icons:0:" + key] = value;
        using var host = Host(settings);
        var error = await Assert.ThrowsAnyAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Contains("Mcp:Server:Icons", error.Message);
        Assert.DoesNotContain(value, error.ToString());
    }

    [Fact]
    public async Task MissingSections_UseExistingDefaultsAndSingleDefaultIcon()
    {
        using var host = Host(new());
        await host.StartAsync();
        Assert.Equal("http://192.168.0.120", host.Services.GetRequiredService<IOptions<UltimateClientOptions>>().Value.BaseUrl);
        Assert.False(host.Services.GetRequiredService<IOptions<McpOAuthOptions>>().Value.Enabled);
        Assert.Single(host.Services.GetRequiredService<IOptions<McpServerIdentityOptions>>().Value.Icons);
        Assert.Equal(256, host.Services.GetRequiredService<IOptions<McpTaskStoreOptions>>().Value.Capacity);
        await host.StopAsync();
    }

    [Fact]
    public async Task ShippedJsonAndRegisteredDefaults_AgreeForEveryApplicationSection()
    {
        using var defaults = Host(new());
        using var configured = Host(new(), jsonFile: System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        await defaults.StartAsync();
        await configured.StartAsync();
        Type[] types = [typeof(UltimateClientOptions), typeof(DownloadOptions), typeof(ProgramArtifactOptions),
            typeof(McpTaskStoreOptions), typeof(ProgramWorkflowOptions), typeof(LiveResourceOptions), typeof(ContentCacheOptions),
            typeof(McpAppsOptions), typeof(McpNotificationOptions), typeof(McpTransportOptions), typeof(McpServerIdentityOptions), typeof(McpOAuthOptions)];
        foreach (var type in types)
        {
            var optionsType = typeof(IOptions<>).MakeGenericType(type);
            var property = optionsType.GetProperty("Value")!;
            Assert.Equal(JsonSerializer.Serialize(property.GetValue(defaults.Services.GetRequiredService(optionsType)), type),
                JsonSerializer.Serialize(property.GetValue(configured.Services.GetRequiredService(optionsType)), type));
        }
        await configured.StopAsync();
        await defaults.StopAsync();
    }

    [Fact]
    public void CompatibilityConstructor_ValidatesDownloadSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Downloads:RequestTimeout"] = "00:00:00" }).Build();
        Assert.Throws<OptionsValidationException>(() => new UltimateService(NullLogger<UltimateService>.Instance,
            NullLogger<Ultimate64Client>.Instance, config));
    }

    [Fact]
    public async Task ValidMinimumValues_AndCliPrecedenceAreAccepted()
    {
        using var host = Host(new()
        {
            ["Ultimate:RequestTimeout"] = "00:00:00.100", ["Mcp:Artifacts:MaxEntries"] = "1",
            ["Mcp:Artifacts:MaxBytes"] = "2", ["Mcp:Artifacts:MaxArtifactBytes"] = "2",
            ["Mcp:Artifacts:Lifetime"] = "00:00:01", ["Mcp:Workflow:ContinuationLifetime"] = "00:00:01",
            ["Mcp:Tasks:Capacity"] = "1", ["Mcp:Tasks:TerminalRetention"] = "00:00:01",
            ["Mcp:Tasks:PollInterval"] = "00:00:00.010", ["Mcp:LiveResources:PollInterval"] = "00:00:00.100",
            ["Mcp:LiveResources:ListenerQueueCapacity"] = "2", ["Mcp:Cache:DocumentationTimeToLive"] = "00:00:00",
            ["Mcp:Apps:PreviewBytes"] = "1", ["Host:ShutdownTimeout"] = "00:00:00.100",
            ["Mcp:Notifications:CleanupTimeout"] = "00:00:00.100"
        }, ["--Ultimate:BaseUrl=http://cli.example"]);
        await host.StartAsync();
        Assert.Equal("http://cli.example", host.Services.GetRequiredService<IOptions<UltimateClientOptions>>().Value.BaseUrl);
        await host.StopAsync();
    }

    [Fact]
    public async Task UltimateEnvironmentVariable_OverridesJsonAndFeedsEveryDeviceConsumer()
    {
        var previous = Environment.GetEnvironmentVariable("Ultimate__BaseUrl");
        try
        {
            Environment.SetEnvironmentVariable("Ultimate__BaseUrl", "http://env-device.example:8123");
            using var factory = new McpAppFactory();
            var calls = 0;
            factory.Device.Override = (request, _) =>
            {
                Assert.Equal("env-device.example", request.RequestUri!.Host);
                Assert.Equal(8123, request.RequestUri.Port);
                Interlocked.Increment(ref calls);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"version\":\"test\",\"errors\":[]}") });
            };
            using var rpc = new RpcClient(factory.CreateClient(), true);
            await rpc.ConnectAsync();
            Assert.Equal("http://env-device.example:8123", factory.Services.GetRequiredService<IOptions<UltimateClientOptions>>().Value.BaseUrl);
            await rpc.CallAsync("resources/read", new() { ["uri"] = DeviceResourceHub.StatusUri });
            await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_version", ["arguments"] = new JsonObject() });
            Assert.Equal(2, calls);
            var config = factory.Services.GetRequiredService<IConfiguration>();
            config["Ultimate:BaseUrl"] = "http://changed.example";
            ((IConfigurationRoot)config).Reload();
            Assert.Equal("http://env-device.example:8123", factory.Services.GetRequiredService<UltimateDeviceClientFactory>().Target);
            await rpc.CallAsync("resources/read", new() { ["uri"] = DeviceResourceHub.StatusUri });
        }
        finally { Environment.SetEnvironmentVariable("Ultimate__BaseUrl", previous); }
    }

    [Fact]
    public void MalformedEnvironmentUrl_PreventsAppStartupBeforeDeviceHttp()
    {
        var previous = Environment.GetEnvironmentVariable("Ultimate__BaseUrl");
        try
        {
            Environment.SetEnvironmentVariable("Ultimate__BaseUrl", "not-a-url");
            using var factory = new McpAppFactory();
            var calls = 0;
            factory.Device.Override = (_, _) => { Interlocked.Increment(ref calls); throw new InvalidOperationException(); };
            Assert.ThrowsAny<OptionsValidationException>(() => factory.CreateClient());
            Assert.Equal(0, calls);
        }
        finally { Environment.SetEnvironmentVariable("Ultimate__BaseUrl", previous); }
    }

    [Fact]
    public async Task NondefaultArtifactTaskCacheAndPreviewSettings_AffectHttpResults()
    {
        using var factory = new McpAppFactory(settings: new Dictionary<string, string?>
        {
            ["Mcp:Artifacts:MaxEntries"] = "1", ["Mcp:Artifacts:Lifetime"] = "00:00:02",
            ["Mcp:Workflow:ContinuationLifetime"] = "00:00:01", ["Mcp:Apps:PreviewBytes"] = "3",
            ["Mcp:Tasks:Capacity"] = "1", ["Mcp:Tasks:TerminalRetention"] = "00:00:02",
            ["Mcp:Tasks:PollInterval"] = "00:00:00.100", ["Mcp:Cache:DocumentationTimeToLive"] = "00:00:07"
        });
        using var rpc = new RpcClient(factory.CreateClient(), true);
        await rpc.ConnectAsync();
        var compile = await rpc.CallAsync("tools/call", Workflow(new() { ["language"] = "basic", ["source"] = "10 END" }));
        var info = compile["structuredContent"]!["artifact"]!;
        var preview = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_program_preview", ["arguments"] = new JsonObject { ["artifactId"] = info["id"]!.GetValue<string>() } });
        Assert.Equal(6, preview["structuredContent"]!["hex"]!.GetValue<string>().Length);
        Assert.Equal(7000, (await rpc.CallAsync("resources/read", new() { ["uri"] = "c64://basic/spec" }))["ttlMs"]!.GetValue<int>());
        var store = factory.Services.GetRequiredService<ProgramArtifactStore>();
        store.Add([1, 8, 96]);
        Assert.Throws<McpProtocolException>(() => store.Get(info["id"]!.GetValue<string>()));
        var task = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_generate_basic_prg", ["arguments"] = new JsonObject { ["basicSource"] = "10 END" } }, tasks: true);
        Assert.Equal(100, task["pollIntervalMs"]!.GetValue<int>());
        await McpFeatureIntegrationTests.PollAsync(rpc, task["taskId"]!.GetValue<string>(), "completed");
        factory.Clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Null(await factory.Services.GetRequiredService<BoundedTaskStore>().GetTaskAsync(task["taskId"]!.GetValue<string>()));
    }

    [Theory]
    [InlineData("initial")]
    [InlineData("elicitation")]
    [InlineData("sampling")]
    public async Task ConfiguredSourceLimit_CoversEveryInputPath(string path)
    {
        using var factory = new McpAppFactory(settings: new Dictionary<string, string?> { ["Mcp:Workflow:MaxSourceBytes"] = "6", ["Mcp:Workflow:SamplingMaxTokens"] = "17" });
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = new() { ["elicitation"] = new JsonObject { ["form"] = new JsonObject() }, ["sampling"] = new JsonObject() } };
        await rpc.ConnectAsync();
        var args = new JsonObject { ["language"] = "basic" };
        if (path == "initial") args["source"] = "10 PRINT 1";
        if (path == "sampling") args["generationRequest"] = "hello";
        var result = await rpc.CallAsync("tools/call", Workflow(args));
        if (path != "initial")
        {
            var field = path == "sampling" ? "sampling" : "source";
            if (path == "sampling") Assert.Equal(17, result["inputRequests"]!["sampling"]!["params"]!["maxTokens"]!.GetValue<int>());
            var answer = path == "sampling"
                ? new JsonObject { ["role"] = "assistant", ["model"] = "test", ["content"] = new JsonObject { ["type"] = "text", ["text"] = "10 PRINT 1" } }
                : new JsonObject { ["action"] = "accept", ["content"] = new JsonObject { ["source"] = "10 PRINT 1" } };
            result = await rpc.CallAsync("tools/call", Workflow(args, result["requestState"], field, answer));
        }
        Assert.Equal("InputTooLarge", result["structuredContent"]!["errorType"]!.GetValue<string>());
        Assert.Equal(0, factory.Device.Executions);
    }

    [Fact]
    public async Task ContinuationDeadline_IsNotExtendedByArtifactCreation()
    {
        using var factory = new McpAppFactory(settings: new Dictionary<string, string?> { ["Mcp:Workflow:ContinuationLifetime"] = "00:00:01" });
        using var rpc = new RpcClient(factory.CreateClient(), true) { Capabilities = new() { ["elicitation"] = new JsonObject { ["form"] = new JsonObject() } } };
        await rpc.ConnectAsync();
        var args = new JsonObject { ["language"] = "basic", ["source"] = "10 END", ["execute"] = true };
        var confirm = await rpc.CallAsync("tools/call", Workflow(args));
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var result = await rpc.CallAsync("tools/call", Workflow(args, confirm["requestState"], "confirm", new JsonObject { ["action"] = "accept", ["content"] = new JsonObject { ["confirm"] = true } }));
        Assert.Equal("InvalidContinuation", result["structuredContent"]!["errorType"]!.GetValue<string>());
        Assert.Equal(0, factory.Device.Executions);
    }

    [Theory]
    [InlineData("StatefulForInitializeClients")]
    [InlineData("Stateful")]
    [InlineData("Stateless")]
    public async Task ConfiguredRoutesIdentityAndSessionMode_AreEffective(string mode)
    {
        using var factory = new McpAppFactory(settings: new Dictionary<string, string?>
        {
            ["Mcp:Transport:EndpointPath"] = "/mcp", ["Mcp:Transport:HealthPath"] = "/status",
            ["Mcp:Transport:SessionMode"] = mode, ["Mcp:Server:Name"] = "Configured C64",
            ["Mcp:Notifications:LegacyLoggingEnabled"] = "false"
        });
        using var http = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/status")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/health")).StatusCode);
        using var modern = new RpcClient(http, true, "/mcp");
        if (mode == "Stateful")
        {
            using var request = modern.CreateRequest("server/discover", new());
            using var response = await http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("-32022", text);
        }
        else
        {
            var discovered = await modern.ConnectAsync();
            Assert.Contains("Configured C64", discovered.ToJsonString());
            Assert.Null(modern.SessionId);
        }
        using var legacy = new RpcClient(factory.CreateClient(), false, "/mcp");
        await legacy.ConnectAsync();
        if (mode == "Stateless") Assert.Null(legacy.SessionId);
        else { Assert.NotNull(legacy.SessionId); await legacy.DeleteSessionAsync(); }
    }

    [Fact]
    public async Task DownloadTransport_UsesOwnTimeoutAndNeverDevicePassword()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new DeviceStub { Override = async (request, ct) =>
        {
            Assert.False(request.Headers.Contains("X-Password"));
            Assert.Equal("source.example", request.RequestUri!.Host);
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException();
        } };
        var downloads = new DownloadClientFactory(Options.Create(new DownloadOptions { RequestTimeout = TimeSpan.FromMilliseconds(100) }), stub.CreateHandler);
        using var http = downloads.Create();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => http.GetAsync("https://source.example/program.prg"));
        Assert.True(started.Task.IsCompleted);
        var device = new UltimateDeviceClientFactory(Options.Create(new UltimateClientOptions { NetworkPassword = "secret", RequestTimeout = TimeSpan.FromMilliseconds(100) }), NullLoggerFactory.Instance, stub.CreateHandler);
        Assert.Equal(TimeSpan.FromMilliseconds(100), device.GetOptions().RequestTimeout);
    }

    [Fact]
    public async Task ConfiguredDeviceTimeout_ActuallyCancelsHttpAndExplicitClientArgumentWins()
    {
        var requests = 0;
        var stub = new DeviceStub { Override = async (request, ct) =>
        {
            Assert.Equal("device.example", request.RequestUri!.Host);
            Assert.Equal("secret", request.Headers.GetValues("X-Password").Single());
            Interlocked.Increment(ref requests);
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException();
        } };
        var settings = new UltimateClientOptions { BaseUrl = "http://device.example", NetworkPassword = "secret", RequestTimeout = TimeSpan.FromMilliseconds(100) };
        var factory = new UltimateDeviceClientFactory(Options.Create(settings), NullLoggerFactory.Instance, stub.CreateHandler);
        using var device = factory.Create();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.GetVersionAsync());
        Assert.Equal(1, requests);
        // The configured timeout is long, so only the retained explicit constructor argument can cancel this.
        settings.RequestTimeout = TimeSpan.FromMinutes(1);
        using var explicitTimeout = new Ultimate64Client(settings, stub.CreateHandler(), timeout: TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => explicitTimeout.GetVersionAsync());
        Assert.Equal(2, requests);
    }

    [Fact]
    public async Task ConfiguredLiveInterval_ControlsPollingAndQueueIsBounded()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var stub = new DeviceStub { Override = (_, _) =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"version\":\"test\",\"errors\":[]}") });
        } };
        using var hub = new DeviceResourceHub(new UltimateDeviceClientFactory(Options.Create(new UltimateClientOptions()), NullLoggerFactory.Instance, stub.CreateHandler), clock,
            Options.Create(new LiveResourceOptions { PollInterval = TimeSpan.FromMilliseconds(100), ListenerQueueCapacity = 2 }));
        using var listener = hub.Watch(() => [DeviceResourceHub.StatusUri]);
        await hub.StartAsync(CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => calls == 1);
        clock.Advance(TimeSpan.FromMilliseconds(99));
        Assert.Equal(1, calls);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await McpHttpIntegrationTests.EventuallyAsync(() => calls == 2);
        Assert.Equal(1, listener.Queue.Reader.Count);
        listener.Dispose();
        clock.Advance(TimeSpan.FromSeconds(1));
        await hub.StopAsync(CancellationToken.None);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task LazyOptions_AreFrozenAtStartupBeforeFirstFeatureCall()
    {
        using var factory = new McpAppFactory(settings: new Dictionary<string, string?>
            { ["Mcp:Apps:PreviewBytes"] = "3", ["Mcp:Cache:DocumentationTimeToLive"] = "00:00:07" });
        using var rpc = new RpcClient(factory.CreateClient(), true);
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        configuration["Mcp:Apps:PreviewBytes"] = "1";
        configuration["Mcp:Cache:DocumentationTimeToLive"] = "00:00:09";
        ((IConfigurationRoot)configuration).Reload();
        await rpc.ConnectAsync();
        var info = factory.Services.GetRequiredService<ProgramArtifactStore>().Add([1, 8, 96, 0]);
        var preview = await rpc.CallAsync("tools/call", new() { ["name"] = "ultimate_program_preview", ["arguments"] = new JsonObject { ["artifactId"] = info.Id } });
        Assert.Equal(6, preview["structuredContent"]!["hex"]!.GetValue<string>().Length);
        var doc = await rpc.CallAsync("resources/read", new() { ["uri"] = "c64://basic/spec" });
        Assert.Equal(7000, doc["ttlMs"]!.GetValue<int>());
    }

    private static IHost Host(Dictionary<string, string?> settings, string[]? arguments = null, string? jsonFile = null)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        if (jsonFile is not null) builder.Configuration.AddJsonFile(jsonFile);
        builder.Configuration.AddInMemoryCollection(settings);
        if (arguments is not null) builder.Configuration.AddCommandLine(arguments);
        builder.Services.AddValidatedApplicationOptions(builder.Configuration);
        return builder.Build();
    }

    private static (string Key, long Min, long Max)[] IntegerLimits() =>
    [
        ("Mcp:Artifacts:MaxEntries", 1, 10000), ("Mcp:Artifacts:MaxBytes", 2, 536870912),
        ("Mcp:Artifacts:MaxArtifactBytes", 2, 67108864), ("Mcp:Tasks:Capacity", 1, 10000),
        ("Mcp:Workflow:Capacity", 1, 10000), ("Mcp:Workflow:MaxRounds", 1, 64),
        ("Mcp:Workflow:MaxSourceBytes", 1, 16777216), ("Mcp:Workflow:MaxGenerationRequestBytes", 1, 1048576),
        ("Mcp:Workflow:SamplingMaxTokens", 1, 32768), ("Mcp:Workflow:MaxRootHints", 1, 1000),
        ("Mcp:LiveResources:ListenerQueueCapacity", 2, 4096), ("Mcp:Apps:PreviewBytes", 1, 256),
        ("Mcp:Transport:MaxIdleSessionCount", 1, 100000)
    ];

    private static (string Key, TimeSpan Min, TimeSpan Max)[] DurationLimits() =>
    [
        ("Ultimate:RequestTimeout", TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(10)),
        ("Downloads:RequestTimeout", TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(10)),
        ("Mcp:Artifacts:Lifetime", TimeSpan.FromSeconds(1), TimeSpan.FromDays(1)),
        ("Mcp:Workflow:ContinuationLifetime", TimeSpan.FromSeconds(1), TimeSpan.FromDays(1)),
        ("Mcp:Tasks:TerminalRetention", TimeSpan.FromSeconds(1), TimeSpan.FromDays(7)),
        ("Mcp:Tasks:PollInterval", TimeSpan.FromMilliseconds(10), TimeSpan.FromMinutes(1)),
        ("Mcp:LiveResources:PollInterval", TimeSpan.FromMilliseconds(100), TimeSpan.FromHours(1)),
        ("Mcp:Cache:DocumentationTimeToLive", TimeSpan.Zero, TimeSpan.FromDays(1)),
        ("Mcp:Notifications:LegacyLoggingInterval", TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(5)),
        ("Mcp:Notifications:CleanupTimeout", TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(5)),
        ("Mcp:Transport:IdleTimeout", TimeSpan.FromSeconds(1), TimeSpan.FromDays(7)),
        ("Host:ShutdownTimeout", TimeSpan.FromMilliseconds(100), TimeSpan.FromDays(1)),
        ("Mcp:OAuth:ClockSkew", TimeSpan.Zero, TimeSpan.FromMinutes(5))
    ];

    private static Dictionary<string, string?> BoundarySettings(bool maximum)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Mcp:OAuth:Enabled"] = "true", ["Mcp:OAuth:Authority"] = "https://issuer.example",
            ["Mcp:OAuth:Audience"] = "https://mcp.example/", ["Mcp:Notifications:LegacyLoggingEnabled"] = "true"
        };
        foreach (var (key, min, max) in IntegerLimits()) settings[key] = (maximum ? max : min).ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var (key, min, max) in DurationLimits()) settings[key] = (maximum ? max : min).ToString("c");
        return settings;
    }

    private static JsonObject Workflow(JsonObject args, JsonNode? state = null, string? input = null, JsonNode? answer = null)
    {
        var call = new JsonObject { ["name"] = "ultimate_program_workflow", ["arguments"] = args.DeepClone() };
        if (state is not null) call["requestState"] = state.DeepClone();
        if (input is not null) call["inputResponses"] = new JsonObject { [input] = answer!.DeepClone() };
        return call;
    }
}

using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using C64UltimateClient;
using C64UltimateMcpServer.Features;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using Xunit;

namespace C64UltimateClient.Tests;

public class FeatureStoreTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"errors\":[8]}")]
    public async Task MalformedDeviceJson_BecomesUnavailableAndObserverCanRecover(string body)
    {
        var stub = new DeviceStub { Override = (_, _) => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        { Content = new System.Net.Http.StringContent(body) }) };
        using var hub = new DeviceResourceHub(new UltimateDeviceClientFactory(new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, stub.CreateHandler), new FakeTimeProvider());
        var invalid = await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None);
        Assert.False(invalid.Available);
        Assert.NotNull(invalid.Error);
        stub.Override = null;
        Assert.True((await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None)).Available);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovingFinalLiveInterest_CancelsPollEvenWithIdleLegacyListener(bool unsubscribe)
    {
        var began = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new DeviceStub { Override = async (_, ct) =>
        {
            began.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            throw new InvalidOperationException();
        } };
        using var hub = new DeviceResourceHub(new UltimateDeviceClientFactory(new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, stub.CreateHandler), new FakeTimeProvider());
        using var idleLegacy = hub.Watch(() => Array.Empty<string>());
        var subscriptions = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
        subscriptions[DeviceResourceHub.StatusUri] = 0;
        using var live = hub.Watch(() => subscriptions.Keys);
        await hub.StartAsync(CancellationToken.None);
        await began.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (unsubscribe) { subscriptions.Clear(); hub.InterestsChanged(); }
        else live.Dispose();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await hub.StopAsync(CancellationToken.None);
        Assert.True(hub.ListenerCount >= 1);
    }
    [Theory]
    [InlineData("8")]
    [InlineData("version")]
    public async Task PasswordRedaction_DoesNotCorruptSuccessfulJson(string password)
    {
        var stub = new DeviceStub { Revision = 8 };
        using var client = new Ultimate64Client(new() { BaseUrl = "http://device.invalid", NetworkPassword = password }, stub.CreateHandler());
        Assert.Equal("0.8", (await client.GetVersionAsync()).Version);
        var drives = await client.GetDrivesInfoAsync();
        Assert.Equal(8, drives.Drives[0].GetProperty("a").GetProperty("bus_id").GetInt32());
        stub.Override = (_, _) => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
        { Content = new System.Net.Http.StringContent(JsonSerializer.Serialize(new { errors = new[] { "Denied: " + password } })) });
        var error = await Assert.ThrowsAsync<UltimateClientException>(() => client.GetVersionAsync());
        Assert.Equal("Denied: [redacted]", Assert.Single(error.ApiErrors!));
    }
    [Fact]
    public async Task FinalListenerDisconnect_CancelsAnInFlightDevicePoll()
    {
        var began = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var polls = 0;
        var stub = new DeviceStub { Override = async (_, ct) =>
        {
            Interlocked.Increment(ref polls);
            began.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            throw new InvalidOperationException();
        } };
        var clock = new FakeTimeProvider();
        using var hub = new DeviceResourceHub(new UltimateDeviceClientFactory(new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, stub.CreateHandler), clock);
        var listener = hub.Watch(() => new[] { DeviceResourceHub.StatusUri });
        await hub.StartAsync(CancellationToken.None);
        await began.Task.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Dispose();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TimeSpan.FromMinutes(1));
        await hub.StopAsync(CancellationToken.None);
        Assert.Equal(1, polls);
    }
    [Fact]
    public void DeviceClient_PreservesPreviouslyCompiledMethodSignatures()
    {
        Assert.NotNull(typeof(Ultimate64Client).GetMethod(nameof(Ultimate64Client.GetVersionAsync), Type.EmptyTypes));
        Assert.NotNull(typeof(Ultimate64Client).GetMethod(nameof(Ultimate64Client.ReadMemoryAsync), [typeof(string), typeof(int)]));
        Assert.NotNull(typeof(Ultimate64Client).GetMethod(nameof(Ultimate64Client.PlaySidBinaryAsync), [typeof(byte[]), typeof(int?)]));
        Assert.NotNull(typeof(Ultimate64Client).GetMethod(nameof(Ultimate64Client.RunProgramBinaryAsync), [typeof(byte[])]));
    }
    [Fact]
    public async Task SlowResourceListener_CoalescesChangesAndDisposesWithoutUnboundedBacklog()
    {
        var stub = new DeviceStub();
        var clock = new FakeTimeProvider();
        using var hub = new DeviceResourceHub(new UltimateDeviceClientFactory(new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, stub.CreateHandler), clock);
        using var listener = hub.Watch(() => new[] { DeviceResourceHub.StatusUri, DeviceResourceHub.DrivesUri });
        for (var i = 0; i < 100; i++)
        {
            stub.Revision = i;
            await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None);
            await hub.ReadAsync(DeviceResourceHub.DrivesUri, CancellationToken.None);
        }
        Assert.Equal(2, listener.Queue.Reader.Count);
        listener.Dispose();
        Assert.Equal(0, hub.ListenerCount);
    }
    [Fact]
    public void Artifacts_PreserveBytesAndChecksumAndCannotBeMutatedByCaller()
    {
        var clock = new FakeTimeProvider();
        var store = new ProgramArtifactStore(clock);
        byte[] bytes = [1, 8, 96];
        var info = store.Add(bytes);
        Assert.Equal(0x0801, info.LoadAddress);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), info.Sha256);
        bytes[2] = 0;
        var copy = store.Get(info.Id).Bytes;
        Assert.Equal(96, copy[2]);
        copy[2] = 0;
        Assert.Equal(96, store.Get(info.Id).Bytes[2]);
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Throws<McpProtocolException>(() => store.Get(info.Id));
    }

    [Fact]
    public void Artifacts_RejectSizeLimitsAndEvictOldestWithoutReusingIds()
    {
        var clock = new FakeTimeProvider();
        var store = new ProgramArtifactStore(clock, Options.Create(new ProgramArtifactOptions { MaxArtifactBytes = 4, MaxBytes = 6, MaxEntries = 2 }));
        var first = store.Add([1, 8, 96]);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = store.Add([1, 8, 96]);
        clock.Advance(TimeSpan.FromSeconds(1));
        var third = store.Add([1, 8, 96]);
        Assert.Throws<McpProtocolException>(() => store.Get(first.Id));
        Assert.NotEqual(first.Id, third.Id);
        Assert.Equal(second.Id, store.Get(second.Id).Info.Id);
        Assert.Throws<ArgumentException>(() => store.Add([1, 8, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => store.Add([]));
    }

    [Fact]
    public async Task TaskRetention_PreservesActiveTasksAndExpiresTerminalTasksOnly()
    {
        var clock = new FakeTimeProvider();
        var store = new BoundedTaskStore(clock, Options.Create(new McpTaskStoreOptions { Capacity = 1 }));
        var first = await store.CreateTaskAsync();
        clock.Advance(TimeSpan.FromHours(1));
        Assert.NotNull(await store.GetTaskAsync(first.TaskId));
        await Assert.ThrowsAsync<McpProtocolException>(() => store.CreateTaskAsync());
        await store.SetCompletedAsync(first.TaskId, JsonSerializer.SerializeToElement(new { success = true }));
        clock.Advance(TimeSpan.FromMinutes(29));
        Assert.NotNull(await store.GetTaskAsync(first.TaskId));
        await Assert.ThrowsAsync<McpProtocolException>(() => store.CreateTaskAsync());
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Null(await store.GetTaskAsync(first.TaskId));
        Assert.NotEqual(first.TaskId, (await store.CreateTaskAsync()).TaskId);
    }

    [Fact]
    public async Task TaskInputs_RejectUnknownKeysAndPreserveTerminalState()
    {
        var store = new BoundedTaskStore(new FakeTimeProvider());
        var info = await store.CreateTaskAsync();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var waiting = store.AwaitInputsAsync(info.TaskId, new Dictionary<string, InputRequest>
        { ["answer"] = InputRequest.ForElicitation(new() { Message = "Confirm" }) }, cancellation.Token);
        Assert.Equal(McpTaskStatus.InputRequired, (await store.GetTaskAsync(info.TaskId))!.Status);
        var response = InputResponse.FromElicitResult(new() { Action = "accept" });
        await Assert.ThrowsAsync<McpProtocolException>(() => store.ResolveInputRequestsAsync(info.TaskId, new Dictionary<string, InputResponse> { ["wrong"] = response }));
        await store.ResolveInputRequestsAsync(info.TaskId, new Dictionary<string, InputResponse> { ["answer"] = response });
        Assert.Same(response, (await waiting)["answer"]);
        Assert.True(await store.SetCancelledAsync(info.TaskId));
        await store.SetCompletedAsync(info.TaskId, JsonSerializer.SerializeToElement(new { success = true }));
        Assert.Equal(McpTaskStatus.Cancelled, (await store.GetTaskAsync(info.TaskId))!.Status);
    }

    [Fact]
    public async Task DeviceClient_PropagatesPasswordCancellationAndHttpErrorDetails()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stub = new DeviceStub
        {
            Override = async (request, ct) =>
            {
                Assert.Equal("secret", string.Join("", request.Headers.GetValues("X-Password")));
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
                throw new InvalidOperationException();
            }
        };
        using var client = new Ultimate64Client(new() { BaseUrl = "http://device.invalid", NetworkPassword = "secret" }, stub.CreateHandler());
        using var cancellation = new CancellationTokenSource();
        var pending = client.GetVersionAsync(cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        stub.Override = (_, _) => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
        { Content = new System.Net.Http.StringContent("{\"errors\":[\"Password required\"]}") });
        var error = await Assert.ThrowsAsync<UltimateClientException>(() => client.GetVersionAsync());
        Assert.Equal(403, error.StatusCode);
        Assert.Contains("Password required", error.ApiErrors!);
    }
}

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using C64UltimateMcpServer.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using Xunit;
using C64UltimateMcpServer.Features;
using Microsoft.Extensions.Configuration;
using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;

namespace C64UltimateClient.Tests;

// These APIs deliberately cover the initialize-based protocol's logging and session lifecycle.
#pragma warning disable MCP9005, MCPEXP002
public class McpNotificationTests
{
    [Fact]
    public async Task ConfiguredLoggingInterval_ControlsActualNotifications()
    {
        var clock = new FakeTimeProvider();
        var messages = new ConcurrentQueue<JsonRpcNotification>();
        var server = Server(messages);
        server.SetupGet(s => s.LoggingLevel).Returns(LoggingLevel.Debug);
        using var worker = new LoggingUpdateMessageSender(server.Object, clock, Options.Create(new McpNotificationOptions
            { LegacyLoggingInterval = TimeSpan.FromSeconds(1) }));
        await worker.StartAsync(CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => messages.Count == 1);
        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Single(messages);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await McpHttpIntegrationTests.EventuallyAsync(() => messages.Count == 2);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DisabledLogging_KeepsResourceWorkerWithoutPeriodicLogTimer()
    {
        var clock = new TrackingClock();
        var messages = new ConcurrentQueue<JsonRpcNotification>();
        var server = Server(messages);
        server.SetupGet(s => s.SessionId).Returns("no-logging");
        server.SetupGet(s => s.LoggingLevel).Returns(LoggingLevel.Debug);
        using var hub = Hub(clock, new DeviceStub());
        server.Setup(s => s.RunAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await McpHttpIntegrationTests.EventuallyAsync(() => hub.ListenerCount == 1);
            Assert.Equal(0, clock.CreatedTimers);
            await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None);
            await McpHttpIntegrationTests.EventuallyAsync(() => messages.Count == 1);
            Assert.Equal("notifications/resources/updated", messages.Single().Method);
        });
        var subscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>();
        subscriptions["no-logging"] = new();
        subscriptions["no-logging"][DeviceResourceHub.StatusUri] = 0;
        var sessions = new McpSessionNotifications(clock, NullLogger<McpSessionNotifications>.Instance, hub,
            Options.Create(new McpNotificationOptions { LegacyLoggingEnabled = false }));
        await sessions.RunAsync(server.Object, subscriptions, CancellationToken.None);
        Assert.Equal(0, hub.ListenerCount);
        Assert.Equal(0, clock.CreatedTimers);
    }
    [Fact]
    public async Task ResourceWorkers_IsolateSubscriptionsAndStopAfterCancellation()
    {
        var clock = new TrackingClock();
        var firstMessages = new ConcurrentQueue<JsonRpcNotification>();
        var secondMessages = new ConcurrentQueue<JsonRpcNotification>();
        var first = Server(firstMessages);
        var second = Server(secondMessages);
        var firstSubscriptions = new ConcurrentDictionary<string, byte>();
        firstSubscriptions[DeviceResourceHub.StatusUri] = 0;
        var secondSubscriptions = new ConcurrentDictionary<string, byte>();
        secondSubscriptions[DeviceResourceHub.DrivesUri] = 0;
        var device = new DeviceStub();
        using var hub = Hub(clock, device);
        using var firstWorker = new SubscriptionMessageSender(first.Object, firstSubscriptions, hub);
        using var secondWorker = new SubscriptionMessageSender(second.Object, secondSubscriptions, hub);
        await firstWorker.StartAsync(CancellationToken.None);
        await secondWorker.StartAsync(CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => hub.ListenerCount == 2);
        await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None);
        await hub.ReadAsync(DeviceResourceHub.DrivesUri, CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => firstMessages.Count == 1 && secondMessages.Count == 1);
        Assert.Equal(DeviceResourceHub.StatusUri, Assert.Single(firstMessages).Params!["uri"]!.GetValue<string>());
        Assert.Equal(DeviceResourceHub.DrivesUri, Assert.Single(secondMessages).Params!["uri"]!.GetValue<string>());
        Assert.All(firstMessages.Concat(secondMessages), n => Assert.Equal("notifications/resources/updated", n.Method));

        firstSubscriptions.Clear();
        device.Revision++;
        await hub.ReadAsync(DeviceResourceHub.StatusUri, CancellationToken.None);
        await hub.ReadAsync(DeviceResourceHub.DrivesUri, CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => secondMessages.Count == 2);
        Assert.Single(firstMessages);
        Assert.Equal(2, secondMessages.Count);
        await firstWorker.StopAsync(CancellationToken.None);
        await secondWorker.StopAsync(CancellationToken.None);
        var timers = clock.CreatedTimers;
        clock.Advance();
        Assert.Single(firstMessages);
        Assert.Equal(2, secondMessages.Count);
        Assert.Equal(timers, clock.CreatedTimers);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(LoggingLevel.Warning, 5)]
    [InlineData(LoggingLevel.Debug, 8)]
    [InlineData(LoggingLevel.Emergency, 1)]
    public async Task Logging_RespectsThresholdIncludingEqualityAndUsesProtocolJson(LoggingLevel? minimum, int expectedCount)
    {
        var messages = new ConcurrentQueue<JsonRpcNotification>();
        var server = Server(messages);
        server.SetupGet(s => s.LoggingLevel).Returns(minimum);
        using var worker = new LoggingUpdateMessageSender(server.Object, TimeProvider.System);
        foreach (var level in Enum.GetValues<LoggingLevel>()) await worker.SendMessageAsync(level, CancellationToken.None);
        Assert.Equal(expectedCount, messages.Count);
        Assert.All(messages, notification =>
        {
            Assert.Equal("notifications/message", notification.Method);
            Assert.NotNull(notification.Params!["level"]);
            Assert.NotNull(notification.Params["data"]);
            Assert.DoesNotContain(notification.Params.AsObject(), p => string.Equals(p.Key, "Level", StringComparison.Ordinal));
        });
        if (minimum is { } levelValue)
            Assert.Equal(levelValue.ToString().ToLowerInvariant(), messages.First().Params!["level"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SessionCleanup_StopsWorkersAndRemovesSubscriptionsOnErrorOrCancellation(bool cancel)
    {
        var messages = new ConcurrentQueue<JsonRpcNotification>();
        var server = Server(messages);
        server.SetupGet(s => s.SessionId).Returns("test-session");
        var clock = new TrackingClock();
        using var cancellation = new CancellationTokenSource();
        var failure = new InvalidOperationException("session failed");
        server.Setup(s => s.RunAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken ct) =>
        {
            await McpHttpIntegrationTests.EventuallyAsync(() => clock.CreatedTimers >= 1);
            if (cancel)
            {
                cancellation.Cancel();
                ct.ThrowIfCancellationRequested();
            }
            throw failure;
        });
        var subscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>();
        subscriptions["test-session"] = new();
        subscriptions["test-session"]["c64://basic/spec"] = 0;
        using var hub = Hub(clock, new DeviceStub());
        var sessions = new McpSessionNotifications(clock, NullLogger<McpSessionNotifications>.Instance, hub);
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sessions.RunAsync(server.Object, subscriptions, cancellation.Token));
        else Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => sessions.RunAsync(server.Object, subscriptions, cancellation.Token)));
        Assert.Empty(subscriptions);
        Assert.Equal(0, sessions.ActiveSessions);
        var count = messages.Count;
        var timers = clock.CreatedTimers;
        clock.Advance();
        Assert.Equal(count, messages.Count);
        Assert.Equal(timers, clock.CreatedTimers);
    }

    private static DeviceResourceHub Hub(TimeProvider clock, DeviceStub device) => new(new UltimateDeviceClientFactory(
        new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, device.CreateHandler), clock);

    private static Mock<McpServer> Server(ConcurrentQueue<JsonRpcNotification> messages)
    {
        var server = new Mock<McpServer>();
        server.Setup(s => s.SendMessageAsync(It.IsAny<JsonRpcMessage>(), It.IsAny<CancellationToken>()))
            .Callback<JsonRpcMessage, CancellationToken>((message, _) => messages.Enqueue(Assert.IsType<JsonRpcNotification>(message)))
            .Returns(Task.CompletedTask);
        return server;
    }

    private sealed class TrackingClock : TimeProvider
    {
        private readonly FakeTimeProvider _clock = new();
        private int _createdTimers;
        internal int CreatedTimers => Volatile.Read(ref _createdTimers);
        public override DateTimeOffset GetUtcNow() => _clock.GetUtcNow();
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = _clock.CreateTimer(callback, state, dueTime, period);
            Interlocked.Increment(ref _createdTimers);
            return timer;
        }
        internal void Advance() => _clock.Advance(TimeSpan.FromMinutes(1));
    }
}
#pragma warning restore MCP9005, MCPEXP002

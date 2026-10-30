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

namespace C64UltimateClient.Tests;

// These APIs deliberately cover the initialize-based protocol's logging and session lifecycle.
#pragma warning disable MCP9005, MCPEXP002
public class McpNotificationTests
{
    [Fact]
    public async Task ResourceWorkers_IsolateSubscriptionsAndStopAfterCancellation()
    {
        var clock = new TrackingClock();
        var firstMessages = new ConcurrentQueue<JsonRpcNotification>();
        var secondMessages = new ConcurrentQueue<JsonRpcNotification>();
        var first = Server(firstMessages);
        var second = Server(secondMessages);
        var firstSubscriptions = new ConcurrentDictionary<string, byte>();
        firstSubscriptions["c64://basic/spec"] = 0;
        var secondSubscriptions = new ConcurrentDictionary<string, byte>();
        secondSubscriptions["c64://memory/map"] = 0;
        using var firstWorker = new SubscriptionMessageSender(first.Object, firstSubscriptions, clock);
        using var secondWorker = new SubscriptionMessageSender(second.Object, secondSubscriptions, clock);
        await firstWorker.StartAsync(CancellationToken.None);
        await secondWorker.StartAsync(CancellationToken.None);
        await McpHttpIntegrationTests.EventuallyAsync(() => clock.CreatedTimers >= 2);
        Assert.Equal("c64://basic/spec", Assert.Single(firstMessages).Params!["uri"]!.GetValue<string>());
        Assert.Equal("c64://memory/map", Assert.Single(secondMessages).Params!["uri"]!.GetValue<string>());
        Assert.All(firstMessages.Concat(secondMessages), n => Assert.Equal("notifications/resources/updated", n.Method));

        firstSubscriptions.Clear();
        clock.Advance();
        await McpHttpIntegrationTests.EventuallyAsync(() => clock.CreatedTimers >= 4);
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
            await McpHttpIntegrationTests.EventuallyAsync(() => clock.CreatedTimers >= 2);
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
        var sessions = new McpSessionNotifications(clock, NullLogger<McpSessionNotifications>.Instance);
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

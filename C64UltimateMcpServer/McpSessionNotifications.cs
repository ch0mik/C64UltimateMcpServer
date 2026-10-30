using System.Collections.Concurrent;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Core;

/// <summary>Owns notification workers for legacy HTTP sessions only.</summary>
public sealed class McpSessionNotifications(TimeProvider timeProvider, ILogger<McpSessionNotifications> logger)
{
    private int _activeSessions;
    internal int ActiveSessions => Volatile.Read(ref _activeSessions);

    public async Task RunAsync(McpServer server,
        ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> subscriptions,
        CancellationToken cancellationToken)
    {
        var sessionId = server.SessionId ?? throw new InvalidOperationException("A legacy session is required.");
        var sessionSubscriptions = subscriptions.GetOrAdd(sessionId, _ => new());
        using var resources = new SubscriptionMessageSender(server, sessionSubscriptions, timeProvider);
        using var logging = new LoggingUpdateMessageSender(server, timeProvider);
        Interlocked.Increment(ref _activeSessions);
        try
        {
            await resources.StartAsync(cancellationToken);
            await logging.StartAsync(cancellationToken);
            await server.RunAsync(cancellationToken);
        }
        finally
        {
            try
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await Task.WhenAll(StopAsync(resources, cleanup.Token), StopAsync(logging, cleanup.Token));
            }
            finally
            {
                subscriptions.TryRemove(sessionId, out _);
                Interlocked.Decrement(ref _activeSessions);
            }
        }
    }

    private async Task StopAsync(BackgroundService worker, CancellationToken cancellationToken)
    {
        try { await worker.StopAsync(cancellationToken); }
        catch (Exception ex)
        {
            // Cleanup of one worker must not prevent the other from stopping or mask the session error.
            logger.LogWarning(ex, "Failed to stop MCP session worker {Worker}", worker.GetType().Name);
        }
    }
}

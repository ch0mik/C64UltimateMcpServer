using System.Collections.Concurrent;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using C64UltimateMcpServer.Features;

namespace C64UltimateMcpServer.Core;

public class SubscriptionMessageSender(McpServer server, ConcurrentDictionary<string, byte> subscriptions, DeviceResourceHub hub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = hub.Watch(() => subscriptions.Keys);
        await foreach (var uri in listener.ReadAllAsync(stoppingToken))
        {
            if (subscriptions.ContainsKey(uri))
                await server.SendNotificationAsync(NotificationMethods.ResourceUpdatedNotification,
                    new ResourceUpdatedNotificationParams
                    {
                        Uri = uri,
                    }, cancellationToken: stoppingToken);
        }
    }
}

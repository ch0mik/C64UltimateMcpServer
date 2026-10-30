using System.Collections.Concurrent;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Core;

public class SubscriptionMessageSender(McpServer server, ConcurrentDictionary<string, byte> subscriptions, TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var uri in subscriptions.Keys)
            {
                await server.SendNotificationAsync(NotificationMethods.ResourceUpdatedNotification,
                    new ResourceUpdatedNotificationParams
                    {
                        Uri = uri,
                    }, cancellationToken: stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), timeProvider, stoppingToken);
        }
    }
}

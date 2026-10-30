using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Core;

// Logging remains available to initialize-based clients; it is deprecated in protocol 2026-07-28.
#pragma warning disable MCP9005
public class LoggingUpdateMessageSender(McpServer server, TimeProvider timeProvider) : BackgroundService
{
    readonly Dictionary<LoggingLevel, string> _loggingLevelMap = new()
    {
        { LoggingLevel.Debug, "Debug-level message" },
        { LoggingLevel.Info, "Info-level message" },
        { LoggingLevel.Notice, "Notice-level message" },
        { LoggingLevel.Warning, "Warning-level message" },
        { LoggingLevel.Error, "Error-level message" },
        { LoggingLevel.Critical, "Critical-level message" },
        { LoggingLevel.Alert, "Alert-level message" },
        { LoggingLevel.Emergency, "Emergency-level message" }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var msgLevel = (LoggingLevel)Random.Shared.Next(_loggingLevelMap.Count);

            await SendMessageAsync(msgLevel, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(15), timeProvider, stoppingToken);
        }
    }

    internal Task SendMessageAsync(LoggingLevel msgLevel, CancellationToken cancellationToken)
    {
        if (server.LoggingLevel is not { } minimumLevel || msgLevel < minimumLevel)
        {
            return Task.CompletedTask;
        }

        var message = new LoggingMessageNotificationParams
        {
            Level = msgLevel,
            Data = System.Text.Json.JsonSerializer.SerializeToElement(_loggingLevelMap[msgLevel]),
        };

        return server.SendNotificationAsync(NotificationMethods.LoggingMessageNotification, message, cancellationToken: cancellationToken);
    }
}
#pragma warning restore MCP9005

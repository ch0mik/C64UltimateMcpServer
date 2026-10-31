using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Configuration;

internal interface IApplicationOptionsSnapshot { void Capture(); }

internal sealed class ApplicationOptionsSnapshot<T>(IOptions<T> options) : IApplicationOptionsSnapshot where T : class
{
    public void Capture() => _ = options.Value;
}

/// <summary>ValidateOnStart validates IOptionsMonitor, whose cache is distinct from IOptions.
/// Capture every IOptions value before other hosted services are constructed, including lazy features.</summary>
internal sealed class ApplicationOptionsSnapshotInitializer : IHostedService
{
    public ApplicationOptionsSnapshotInitializer(IEnumerable<IApplicationOptionsSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots) snapshot.Capture();
    }
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class ApplicationOptionsSnapshotRegistration
{
    internal static void CaptureAtStartup<T>(this IServiceCollection services) where T : class
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ApplicationOptionsSnapshotInitializer>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IApplicationOptionsSnapshot, ApplicationOptionsSnapshot<T>>());
    }
}

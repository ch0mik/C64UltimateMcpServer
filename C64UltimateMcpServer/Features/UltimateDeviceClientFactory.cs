using C64UltimateClient;
using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Features;

public sealed class UltimateDeviceClientFactory
{
    private readonly UltimateClientOptions _settings;
    private readonly ILoggerFactory _loggers;
    private readonly Func<HttpMessageHandler> _handlers;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public UltimateDeviceClientFactory(IOptions<UltimateClientOptions> settings, ILoggerFactory loggers, Func<HttpMessageHandler> handlers)
    {
        var value = settings.Value;
        _settings = new() { BaseUrl = value.BaseUrl, NetworkPassword = value.NetworkPassword, RequestTimeout = value.RequestTimeout };
        _loggers = loggers; _handlers = handlers;
    }

    // Compatibility constructor; binding lives only in the adapter.
    public UltimateDeviceClientFactory(IConfiguration configuration, ILoggerFactory loggers, Func<HttpMessageHandler> handlers)
        : this(LegacyConfigurationAdapter.Ultimate(configuration), loggers, handlers) { }

    public string Target => _settings.BaseUrl;
    public UltimateClientOptions GetOptions() => new()
    {
        BaseUrl = _settings.BaseUrl, NetworkPassword = _settings.NetworkPassword, RequestTimeout = _settings.RequestTimeout
    };
    public Ultimate64Client Create() => Create(GetOptions());
    public Ultimate64Client Create(UltimateClientOptions options) => new(options, _handlers(), _loggers.CreateLogger<Ultimate64Client>());
}

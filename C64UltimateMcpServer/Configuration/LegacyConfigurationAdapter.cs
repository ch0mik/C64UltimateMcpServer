using C64UltimateClient;
using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Configuration;

/// <summary>Compatibility boundary for callers of the old public IConfiguration constructors.</summary>
internal static class LegacyConfigurationAdapter
{
    internal static IOptions<UltimateClientOptions> Ultimate(IConfiguration configuration)
    {
        var settings = new UltimateClientOptions();
        ApplicationOptionsRegistration.Bind(settings, configuration.GetSection("Ultimate"));
        var errors = ApplicationOptionsRegistration.UltimateErrors(settings).ToArray();
        if (errors.Length > 0) throw new OptionsValidationException(Options.DefaultName, typeof(UltimateClientOptions), errors);
        return Options.Create(settings);
    }
    internal static IOptions<DownloadOptions> Downloads(IConfiguration configuration)
    {
        var settings = new DownloadOptions();
        ApplicationOptionsRegistration.Bind(settings, configuration.GetSection("Downloads"));
        var errors = ApplicationOptionsRegistration.DownloadErrors(settings).ToArray();
        if (errors.Length > 0) throw new OptionsValidationException(Options.DefaultName, typeof(DownloadOptions), errors);
        return Options.Create(settings);
    }
}

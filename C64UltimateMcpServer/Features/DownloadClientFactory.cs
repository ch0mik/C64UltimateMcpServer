using C64UltimateMcpServer.Configuration;
using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Features;

/// <summary>Separate transport: device credentials never enter external source downloads.</summary>
public sealed class DownloadClientFactory(IOptions<DownloadOptions> options, Func<HttpMessageHandler>? handlers = null)
{
    private readonly TimeSpan _timeout = options.Value.RequestTimeout;
    public HttpClient Create() => new(handlers?.Invoke() ?? new SocketsHttpHandler(), disposeHandler: true) { Timeout = _timeout };
}

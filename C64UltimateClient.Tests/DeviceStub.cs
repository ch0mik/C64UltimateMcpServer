using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace C64UltimateClient.Tests;

internal sealed class DeviceStub
{
    internal int Revision;
    internal bool Offline;
    internal int Executions;
    internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Override;
    internal HttpMessageHandler CreateHandler() => new Handler(this);

    private sealed class Handler(DeviceStub owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (owner.Override is { } custom) return custom(request, cancellationToken);
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/v1/runners:run_prg") Interlocked.Increment(ref owner.Executions);
            var data = owner.Offline ? "{\"errors\":[\"device offline\"]}" : path == "/v1/version"
                ? $"{{\"version\":\"0.{owner.Revision}\",\"errors\":[]}}"
                : path == "/v1/drives" ? $"{{\"drives\":[{{\"a\":{{\"enabled\":true,\"bus_id\":8,\"image_file\":\"disk{owner.Revision}.d64\"}}}}],\"errors\":[]}}"
                : "{\"errors\":[]}";
            return Task.FromResult(new HttpResponseMessage(owner.Offline ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent(data, Encoding.UTF8, "application/json") });
        }
    }
}

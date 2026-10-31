using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using C64UltimateClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace C64UltimateClient.Tests;

public class DeviceRedirectTests
{
    [Fact]
    public async Task DeviceTransport_DoesNotForwardSecretsOrRepeatCommandsOnRedirect()
    {
        var targetHits = 0;
        var sourceHits = 0;
        var status = 301;
        string destination = "";
        var targetBuilder = WebApplication.CreateBuilder();
        targetBuilder.WebHost.UseUrls("http://127.0.0.1:0"); targetBuilder.Logging.ClearProviders();
        await using var target = targetBuilder.Build();
        target.Run(async ctx => { targetHits++; await ctx.Response.WriteAsync("{}"); });
        await target.StartAsync();
        var sourceBuilder = WebApplication.CreateBuilder();
        sourceBuilder.WebHost.UseUrls("http://127.0.0.1:0"); sourceBuilder.Logging.ClearProviders();
        await using var source = sourceBuilder.Build();
        source.Run(ctx =>
        {
            if (ctx.Request.Path == "/v1/runners:run_prg")
            {
                sourceHits++;
                Assert.Equal("test-secret", ctx.Request.Headers["X-Password"].ToString());
                ctx.Response.StatusCode = status;
                ctx.Response.Headers.Location = destination;
            }
            else targetHits++;
            return Task.CompletedTask;
        });
        await source.StartAsync();
        try
        {
            // Inject a default handler (normally follows redirects): the client must turn that off.
            using var client = new Ultimate64Client(new() { BaseUrl = source.Urls.Single(), NetworkPassword = "test-secret" }, new SocketsHttpHandler());
            foreach (var code in new[] { 301, 302, 303, 307, 308 })
            {
                status = code;
                foreach (var address in new[] { target.Urls.Single(), source.Urls.Single() })
                {
                    destination = address + "/redirect-target";
                    var error = await Assert.ThrowsAsync<UltimateClientException>(() => client.RunProgramBinaryAsync([1, 8, 96]));
                    Assert.Equal(code, error.StatusCode);
                }
            }
            Assert.Equal(10, sourceHits);
            Assert.Equal(0, targetHits);
        }
        finally { await source.StopAsync(); await target.StopAsync(); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using C64UltimateMcpServer.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace C64UltimateClient.Tests;

public class McpOAuthTests
{
    [Fact]
    public async Task InvalidEnabledConfiguration_PreventsHostStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:OAuth:Enabled"] = "true", ["Mcp:OAuth:Authority"] = "http://invalid.example",
            ["Mcp:OAuth:Audience"] = "", ["Mcp:OAuth:RequiredScopes:0"] = "bad scope"
        });
        builder.Services.AddOptionalMcpOAuth(builder.Configuration);
        using var host = builder.Build();
        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Equal(3, error.Failures.Count());
        Assert.Contains("Mcp:OAuth:Authority", error.Message);
        Assert.Contains("Mcp:OAuth:Audience", error.Message);
        Assert.Contains("Mcp:OAuth:RequiredScopes", error.Message);
        Assert.DoesNotContain("invalid.example", error.Message);
    }

    [Theory]
    [InlineData("Authority", "http://issuer.example")]
    [InlineData("Authority", "https://user:secret@issuer.example")]
    [InlineData("Authority", "https://issuer.example?query=1")]
    [InlineData("Authority", " https://issuer.example")]
    [InlineData("Audience", "")]
    [InlineData("Audience", "https://mcp.example/#fragment")]
    [InlineData("RequiredScopes:0", "")]
    [InlineData("RequiredScopes:0", "read write")]
    [InlineData("RequiredScopes:0", "read\"write")]
    [InlineData("RequiredScopes:1", "mcp:access")]
    public void InvalidSettings_AreRejectedByBoundIOptions(string key, string value)
    {
        var config = OAuthFactory.Settings(true);
        config["Mcp:OAuth:" + key] = value;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptionalMcpOAuth(new ConfigurationBuilder().AddInMemoryCollection(config).Build());
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<McpOAuthOptions>>().Value);
    }

    [Fact]
    public async Task DisabledOAuth_AllowsAnonymousMcpAndDoesNotAdvertiseMetadata()
    {
        using var factory = new OAuthFactory(false);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(Discover());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var metadata = await client.GetAsync("/.well-known/oauth-protected-resource");
        Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);
    }

    [Fact]
    public async Task EnabledOAuth_ChallengesWithMetadataAndKeepsHealthPublic()
    {
        using var factory = new OAuthFactory(true);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(Discover());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata", response.Headers.WwwAuthenticate.ToString());
        Assert.Contains("resource_metadata=\"https://mcp.example/.well-known/oauth-protected-resource\"", response.Headers.WwwAuthenticate.ToString());
        Assert.Contains("scope=\"mcp:access\"", response.Headers.WwwAuthenticate.ToString());
        using var metadata = await client.GetAsync("/.well-known/oauth-protected-resource");
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        var json = JsonNode.Parse(await metadata.Content.ReadAsStringAsync())!;
        Assert.Equal(OAuthFactory.Audience, json["resource"]!.GetValue<string>());
        Assert.Equal(OAuthFactory.Authority, json["authorization_servers"]![0]!.GetValue<string>());
        Assert.Equal("mcp:access", json["scopes_supported"]![0]!.GetValue<string>());
        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Theory]
    [InlineData("valid", 200)]
    [InlineData("scp", 200)]
    [InlineData("scope-case", 403)]
    [InlineData("missing-scope", 403)]
    [InlineData("issuer", 401)]
    [InlineData("audience", 401)]
    [InlineData("expired", 401)]
    [InlineData("signature", 401)]
    [InlineData("malformed", 401)]
    public async Task BearerTokens_AreValidatedBeforeMcpDispatch(string variant, int expectedStatus)
    {
        using var factory = new OAuthFactory(true);
        using var client = factory.CreateClient();
        using var request = Discover();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(variant));
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)expectedStatus, response.StatusCode);
        if (expectedStatus == 403)
        {
            Assert.Contains("error=\"insufficient_scope\"", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains("scope=\"mcp:access\"", response.Headers.WwwAuthenticate.ToString());
        }
    }

    [Fact]
    public async Task LegacyInitialize_RequiresTheSameAuthorization()
    {
        using var factory = new OAuthFactory(true);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"auth-test","version":"1"}}}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));
    }

    [Theory]
    [InlineData("valid", 403)]
    [InlineData("both-scopes", 200)]
    public async Task EveryConfiguredScope_IsRequired(string variant, int status)
    {
        using var factory = new OAuthFactory(true, scopes: ["mcp:access", "mcp:control"]);
        using var client = factory.CreateClient();
        using var request = Discover();
        request.Headers.Authorization = new("Bearer", factory.Token(variant));
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        if (status == 403) Assert.Contains("scope=\"mcp:access mcp:control\"", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task PublicResourcePath_IsUsedForChallengeAndMetadataOverInternalHttp()
    {
        const string resource = "https://public.example/c64/mcp";
        using var factory = new OAuthFactory(true, resource);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(Discover());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata=\"https://public.example/.well-known/oauth-protected-resource/c64/mcp\"", response.Headers.WwwAuthenticate.ToString());
        using var metadata = await client.GetAsync("/.well-known/oauth-protected-resource/c64/mcp");
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        Assert.Equal(resource, JsonNode.Parse(await metadata.Content.ReadAsStringAsync())!["resource"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfiguredMcpRoute_ProtectsPostGetDeleteAndLeavesHealthPublic()
    {
        using var factory = new OAuthFactory(true, endpointPath: "/c64", healthPath: "/status");
        using var client = factory.CreateClient();
        using var rpc = new RpcClient(client, true, "/c64");
        foreach (var method in new[] { "GET", "DELETE", "POST" })
        {
            using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/c64"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        using var health = await client.GetAsync("/status");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token("valid"));
        await rpc.ConnectAsync();
        using var oldRoute = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/"));
        Assert.Equal(HttpStatusCode.NotFound, oldRoute.StatusCode);
    }

    [Theory]
    [InlineData("00:00:00", 401)]
    [InlineData("00:05:00", 200)]
    public async Task ConfiguredClockSkew_IsAppliedToActualJwtValidation(string clockSkew, int expectedStatus)
    {
        using var factory = new OAuthFactory(true, clockSkew: clockSkew);
        using var client = factory.CreateClient();
        var jwt = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal(TimeSpan.Parse(clockSkew), jwt.TokenValidationParameters.ClockSkew);
        using var request = Discover();
        request.Headers.Authorization = new("Bearer", factory.Token("recently-expired"));
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task LegacyStreamAndDelete_AlsoRequireAuthorization(string method)
    {
        using var factory = new OAuthFactory(true);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage Discover()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"server/discover","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"auth-test","version":"1"},"io.modelcontextprotocol/clientCapabilities":{}}}}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("MCP-Protocol-Version", "2026-07-28");
        request.Headers.Add("Mcp-Method", "server/discover");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return request;
    }
}

internal sealed class OAuthFactory(bool enabled, string audience = OAuthFactory.Audience, string[]? scopes = null,
    string endpointPath = "/", string healthPath = "/health", string clockSkew = "00:00:30") : WebApplicationFactory<Program>
{
    internal const string Authority = "https://issuer.example";
    internal const string Audience = "https://mcp.example/";
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes("test-only-signing-key-with-at-least-32-bytes-123456"));

    internal static Dictionary<string, string?> Settings(bool enabled) => new()
    {
        ["Mcp:OAuth:Enabled"] = enabled.ToString(),
        ["Mcp:OAuth:Authority"] = enabled ? Authority : "",
        ["Mcp:OAuth:Audience"] = enabled ? Audience : "",
        ["Mcp:OAuth:RequiredScopes:0"] = "mcp:access"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = Settings(enabled);
            settings["Mcp:Transport:EndpointPath"] = endpointPath;
            settings["Mcp:Transport:HealthPath"] = healthPath;
            settings["Mcp:OAuth:ClockSkew"] = clockSkew;
            if (enabled) settings["Mcp:OAuth:Audience"] = audience;
            if (scopes is not null)
                for (var i = 0; i < scopes.Length; i++) settings[$"Mcp:OAuth:RequiredScopes:{i}"] = scopes[i];
            config.AddInMemoryCollection(settings);
        });
        builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt =>
        {
            // Static test-only discovery/signing keys: no network or real identity provider.
            var discovery = new OpenIdConnectConfiguration { Issuer = Authority };
            discovery.SigningKeys.Add(_key);
            jwt.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
        }));
    }

    internal string Token(string variant)
    {
        if (variant == "malformed") return "not-a-jwt";
        var key = variant == "signature" ? new SymmetricSecurityKey(new byte[48]) : _key;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = variant == "issuer" ? "https://other.example" : Authority,
            Audience = variant == "audience" ? "https://other.example" : audience,
            IssuedAt = DateTime.UtcNow.AddMinutes(-10), NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = variant == "expired" ? DateTime.UtcNow.AddMinutes(-5)
                : variant == "recently-expired" ? DateTime.UtcNow.AddSeconds(-30) : DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity([new Claim("sub", "test-user"), new Claim(variant == "scp" ? "scp" : "scope",
                variant == "missing-scope" ? "other" : variant == "scope-case" ? "MCP:ACCESS" : "other mcp:access"),
                new Claim("scp", variant == "both-scopes" ? "mcp:control" : "other")]),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
    }
}

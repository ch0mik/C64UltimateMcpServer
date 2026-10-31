using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;

namespace C64UltimateMcpServer.Configuration;

public static class McpOAuthConfiguration
{
    public const string AccessPolicy = "McpOAuthAccess";

    public static IServiceCollection AddOptionalMcpOAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.CaptureAtStartup<McpOAuthOptions>();
        services.AddSingleton<IValidateOptions<McpOAuthOptions>, McpOAuthOptionsValidator>();
        services.AddOptions<McpOAuthOptions>().Configure(options =>
        {
            var section = configuration.GetSection(McpOAuthOptions.SectionName);
            ApplicationOptionsRegistration.Bind(options, section);
            if (!ApplicationOptionsRegistration.HasKey(section, "RequiredScopes")) options.RequiredScopes = ["mcp:access"];
        }).ValidateOnStart();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        }).AddJwtBearer().AddMcp();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<McpOAuthOptions>>((jwt, settings) =>
            {
                var oauth = settings.Value;
                if (!oauth.Enabled) return;
                jwt.Authority = oauth.Authority;
                jwt.Audience = oauth.Audience;
                jwt.RequireHttpsMetadata = true;
                jwt.MapInboundClaims = false;
                jwt.IncludeErrorDetails = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = oauth.Authority,
                    ValidateAudience = true, ValidAudience = oauth.Audience,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                    ClockSkew = oauth.ClockSkew
                };
                // Use the configured public URL, even when TLS terminates at a reverse proxy.
                var metadataUri = MetadataUri(oauth);
                var scopes = string.Join(' ', oauth.RequiredScopes);
                jwt.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{metadataUri}\", scope=\"{scopes}\"";
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.Headers.WWWAuthenticate = $"Bearer error=\"insufficient_scope\", scope=\"{scopes}\", resource_metadata=\"{metadataUri}\"";
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<McpOAuthOptions>>((mcp, settings) =>
            {
                var oauth = settings.Value;
                if (oauth.Enabled)
                {
                    // Route challenge/forbid through the configured JWT events. Keep metadata
                    // matching relative so an internal HTTP hop does not fail SDK host/scheme checks.
                    mcp.ForwardChallenge = JwtBearerDefaults.AuthenticationScheme;
                    mcp.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
                    mcp.ResourceMetadataUri = new Uri(MetadataUri(oauth).AbsolutePath, UriKind.Relative);
                    mcp.ResourceMetadata = new()
                    {
                        Resource = oauth.Audience,
                        AuthorizationServers = { oauth.Authority },
                        ScopesSupported = [.. oauth.RequiredScopes]
                    };
                }
                else
                {
                    // The SDK handler also serves metadata; disabled auth must not advertise it.
                    mcp.Events.OnResourceMetadataRequest = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                }
            });
        services.AddAuthorizationBuilder().AddPolicy(AccessPolicy, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                var settings = context.Resource is HttpContext http
                    ? http.RequestServices.GetRequiredService<IOptions<McpOAuthOptions>>().Value
                    : null;
                if (settings is not { Enabled: true }) return false;
                var granted = context.User.FindAll("scope").Concat(context.User.FindAll("scp"))
                    .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .ToHashSet(StringComparer.Ordinal);
                return settings.RequiredScopes.All(granted.Contains);
            });
        });
        return services;
    }

    private static Uri MetadataUri(McpOAuthOptions options)
    {
        var resource = new Uri(options.Audience);
        return new Uri(resource.GetLeftPart(UriPartial.Authority) + "/.well-known/oauth-protected-resource" + resource.AbsolutePath.TrimEnd('/'));
    }
}

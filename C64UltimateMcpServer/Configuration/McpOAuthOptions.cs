using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Configuration;

/// <summary>Optional OAuth resource-server settings, fixed for the lifetime of the process.</summary>
public sealed class McpOAuthOptions
{
    public const string SectionName = "Mcp:OAuth";
    public bool Enabled { get; set; }
    public string Authority { get; set; } = "";
    // Also the public resource identifier advertised to MCP clients (RFC 8707).
    public string Audience { get; set; } = "";
    // Start empty: ConfigurationBinder appends to initialized arrays.
    public string[] RequiredScopes { get; set; } = [];
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed class McpOAuthOptionsValidator : IValidateOptions<McpOAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, McpOAuthOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        List<string> errors = [];
        if (options.ClockSkew < TimeSpan.Zero || options.ClockSkew > TimeSpan.FromMinutes(5))
            errors.Add("Mcp:OAuth:ClockSkew must be between zero and five minutes.");
        if (!IsHttpsUri(options.Authority))
            errors.Add("Mcp:OAuth:Authority must be an absolute HTTPS URL without credentials, query or fragment.");
        if (!IsHttpsUri(options.Audience))
            errors.Add("Mcp:OAuth:Audience must be the absolute public HTTPS resource URL without credentials, query or fragment.");
        if (options.RequiredScopes is not { Length: > 0 } || options.RequiredScopes.Any(scope =>
                string.IsNullOrWhiteSpace(scope) || scope.Any(c => c < 0x21 || c > 0x7e || c is '"' or '\\')))
            errors.Add("Mcp:OAuth:RequiredScopes must contain at least one non-empty OAuth scope token (no whitespace, quotes or backslashes).");
        else if (options.RequiredScopes.Distinct(StringComparer.Ordinal).Count() != options.RequiredScopes.Length)
            errors.Add("Mcp:OAuth:RequiredScopes must not contain duplicates.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static bool IsHttpsUri(string? value) => !string.IsNullOrWhiteSpace(value)
        && value == value.Trim()
        && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && !string.IsNullOrEmpty(uri.Host) && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;
}

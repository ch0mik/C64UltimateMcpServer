using C64UltimateClient;
using Microsoft.Extensions.Options;

namespace C64UltimateMcpServer.Configuration;

/// <summary>The only binding boundary for application configuration. Error messages never echo values.</summary>
public static class ApplicationOptionsRegistration
{
    public static IServiceCollection AddValidatedApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.CaptureAtStartup<ConfigurationNamespace>();
        services.AddOptions<ConfigurationNamespace>().Configure(_ =>
        {
            string[] known = ["OAuth", "Artifacts", "Tasks", "Workflow", "LiveResources", "Cache", "Apps", "Notifications", "Transport", "Server"];
            if (configuration["Mcp"] is not null || configuration.GetSection("Mcp").GetChildren().Any(c => !known.Contains(c.Key, StringComparer.OrdinalIgnoreCase)))
                throw Failure<ConfigurationNamespace>("Mcp", "contains an unknown subsection.");
        }).ValidateOnStart();
        services.AddOptions<HostOptions>().Configure(o => Bind(o, configuration.GetSection("Host")))
            .Validate(o => Duration(o.ShutdownTimeout, 0.1, 86400), "Host:ShutdownTimeout must be finite and between 100 ms and one day.")
            .ValidateOnStart();

        Register<UltimateClientOptions>(services, configuration, "Ultimate", UltimateErrors);
        Register<DownloadOptions>(services, configuration, "Downloads", DownloadErrors);
        Register<ProgramArtifactOptions>(services, configuration, "Mcp:Artifacts", o => Errors(
            (Count(o.MaxEntries), "Mcp:Artifacts:MaxEntries"),
            (o.MaxBytes is >= 2 and <= 536870912, "Mcp:Artifacts:MaxBytes"),
            (o.MaxArtifactBytes is >= 2 and <= 67108864 && o.MaxArtifactBytes <= o.MaxBytes, "Mcp:Artifacts:MaxArtifactBytes"),
            (Duration(o.Lifetime, 1, 86400), "Mcp:Artifacts:Lifetime")));
        Register<McpTaskStoreOptions>(services, configuration, "Mcp:Tasks", o => Errors(
            (Count(o.Capacity), "Mcp:Tasks:Capacity"),
            (Duration(o.TerminalRetention, 1, 604800), "Mcp:Tasks:TerminalRetention"),
            (Duration(o.PollInterval, .01, 60) && o.PollInterval < o.TerminalRetention, "Mcp:Tasks:PollInterval")));
        Register<ProgramWorkflowOptions>(services, configuration, "Mcp:Workflow", o => Errors(
            (Count(o.Capacity), "Mcp:Workflow:Capacity"), (Duration(o.ContinuationLifetime, 1, 86400), "Mcp:Workflow:ContinuationLifetime"),
            (o.MaxRounds is >= 1 and <= 64, "Mcp:Workflow:MaxRounds"),
            (o.MaxSourceBytes is >= 1 and <= 16777216, "Mcp:Workflow:MaxSourceBytes"),
            (o.MaxGenerationRequestBytes is >= 1 and <= 1048576, "Mcp:Workflow:MaxGenerationRequestBytes"),
            (o.SamplingMaxTokens is >= 1 and <= 32768, "Mcp:Workflow:SamplingMaxTokens"),
            (o.MaxRootHints is >= 1 and <= 1000, "Mcp:Workflow:MaxRootHints")))
            .Validate<IOptions<ProgramArtifactOptions>>((o, artifacts) => o.ContinuationLifetime <= artifacts.Value.Lifetime,
                "Mcp:Workflow:ContinuationLifetime must not exceed Mcp:Artifacts:Lifetime.");
        Register<LiveResourceOptions>(services, configuration, "Mcp:LiveResources", o => Errors(
            (Duration(o.PollInterval, .1, 3600), "Mcp:LiveResources:PollInterval"),
            (o.ListenerQueueCapacity is >= 2 and <= 4096, "Mcp:LiveResources:ListenerQueueCapacity")));
        Register<ContentCacheOptions>(services, configuration, "Mcp:Cache", o => Errors(
            (Duration(o.DocumentationTimeToLive, 0, 86400), "Mcp:Cache:DocumentationTimeToLive")));
        Register<McpAppsOptions>(services, configuration, "Mcp:Apps", o => Errors(
            (o.PreviewBytes is >= 1 and <= 256, "Mcp:Apps:PreviewBytes")));
        Register<McpNotificationOptions>(services, configuration, "Mcp:Notifications", o => Errors(
            (!o.LegacyLoggingEnabled || Duration(o.LegacyLoggingInterval, .1, 300), "Mcp:Notifications:LegacyLoggingInterval"),
            (Duration(o.CleanupTimeout, .1, 300), "Mcp:Notifications:CleanupTimeout")))
            .Validate<IOptions<HostOptions>>((o, host) => o.CleanupTimeout <= host.Value.ShutdownTimeout,
                "Mcp:Notifications:CleanupTimeout must not exceed Host:ShutdownTimeout.");
        Register<McpTransportOptions>(services, configuration, "Mcp:Transport", TransportErrors);
        Register<McpServerIdentityOptions>(services, configuration, "Mcp:Server", IdentityErrors, (o, section) =>
        {
            if (!HasKey(section, "Icons")) o.Icons = McpServerIdentityOptions.DefaultIcons();
        });
        services.AddOptionalMcpOAuth(configuration);
        return services;
    }

    internal static OptionsBuilder<T> Register<T>(IServiceCollection services, IConfiguration configuration, string sectionName,
        Func<T, IEnumerable<string>> validate, Action<T, IConfigurationSection>? defaults = null) where T : class
    {
        var section = configuration.GetSection(sectionName);
        services.CaptureAtStartup<T>();
        services.AddSingleton<IValidateOptions<T>>(new Validator<T>(validate));
        return services.AddOptions<T>().Configure(o =>
        {
            Bind(o, section);
            defaults?.Invoke(o, section);
        }).ValidateOnStart();
    }

    internal static void Bind<T>(T options, IConfigurationSection section) where T : class
    {
        try
        {
            ValidateShape(typeof(T), section);
            section.Bind(options, o => o.ErrorOnUnknownConfiguration = true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException or OverflowException)
        { throw Failure<T>(section.Path, "has an invalid type or unknown key. Check the documented configuration schema."); }
    }

    private static void ValidateShape(Type type, IConfigurationSection section)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var children = section.GetChildren().ToArray();
        if (type == typeof(string) || type.IsValueType || type == typeof(Uri))
        {
            if (children.Length != 0) throw new InvalidOperationException();
            return;
        }
        // Binder can silently ignore scalar values for objects/arrays, even with strict binding.
        if (section.Value is not null) throw new InvalidOperationException();
        var element = type.IsArray ? type.GetElementType() : type.GetInterfaces().Append(type)
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
        if (element is not null)
        {
            foreach (var child in children)
            {
                if (!int.TryParse(child.Key, out var index) || index < 0) throw new InvalidOperationException();
                ValidateShape(element, child);
            }
            return;
        }
        foreach (var child in children)
        {
            var property = type.GetProperties().FirstOrDefault(p => p.Name.Equals(child.Key, StringComparison.OrdinalIgnoreCase));
            if (property is null) throw new InvalidOperationException();
            ValidateShape(property.PropertyType, child);
        }
    }

    internal static bool HasKey(IConfiguration section, string key) => section.GetChildren().Any(c => c.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    internal static OptionsValidationException Failure<T>(string key, string message) => new(Options.DefaultName, typeof(T), [$"{key} {message}"]);
    internal static bool Duration(TimeSpan value, double minimumSeconds, double maximumSeconds) => value.TotalSeconds >= minimumSeconds && value.TotalSeconds <= maximumSeconds;
    private static bool Count(int n) => n is >= 1 and <= 10000;
    private static IEnumerable<string> Errors(params (bool Valid, string Key)[] checks) => checks.Where(c => !c.Valid).Select(c => $"{c.Key} is invalid or outside the documented range.");
    internal static IEnumerable<string> UltimateErrors(UltimateClientOptions o) => Errors(
        (Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) && o.BaseUrl == o.BaseUrl.Trim()
            && uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host) && uri.UserInfo.Length == 0
            && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.AbsolutePath == "/", "Ultimate:BaseUrl"),
        (o.NetworkPassword is null || o.NetworkPassword.All(c => c is >= ' ' and <= '~'), "Ultimate:NetworkPassword"),
        (Duration(o.RequestTimeout, .1, 600), "Ultimate:RequestTimeout"));
    internal static IEnumerable<string> DownloadErrors(DownloadOptions o) => Errors(
        (Duration(o.RequestTimeout, .1, 600), "Downloads:RequestTimeout"));

    private static bool PathValid(string? path) => !string.IsNullOrWhiteSpace(path) && path.StartsWith('/')
        && (path == "/" || !path.EndsWith('/')) && !path.Contains("//")
        && !path.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || "?#%{}*\\".Contains(c))
        && !path.Split('/').Any(p => p is "." or "..")
        && !path.StartsWith("/.well-known/oauth-protected-resource", StringComparison.OrdinalIgnoreCase);
    private static IEnumerable<string> TransportErrors(McpTransportOptions o)
    {
        // The SDK reserves generated /sse and /message routes even though legacy SSE stays disabled.
        var basePath = o.EndpointPath?.TrimEnd('/') ?? "";
        return Errors((PathValid(o.EndpointPath), "Mcp:Transport:EndpointPath"), (PathValid(o.HealthPath), "Mcp:Transport:HealthPath"),
            (!string.Equals(o.EndpointPath, o.HealthPath, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(o.HealthPath, basePath + "/sse", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(o.HealthPath, basePath + "/message", StringComparison.OrdinalIgnoreCase), "Mcp:Transport endpoint collision"),
            (o.SessionMode is "Stateless" or "Stateful" or "StatefulForInitializeClients", "Mcp:Transport:SessionMode"),
            (Duration(o.IdleTimeout, 1, 604800), "Mcp:Transport:IdleTimeout"),
            (o.MaxIdleSessionCount is >= 1 and <= 100000, "Mcp:Transport:MaxIdleSessionCount"));
    }
    private static bool Https(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && value == value.Trim() && !value.Any(char.IsControl)
        && uri.Scheme == "https" && uri.UserInfo.Length == 0 && !string.IsNullOrEmpty(uri.Host);
    private static IEnumerable<string> IdentityErrors(McpServerIdentityOptions o) => Errors(
        (!string.IsNullOrWhiteSpace(o.Name), "Mcp:Server:Name"), (!string.IsNullOrWhiteSpace(o.Version), "Mcp:Server:Version"),
        (o.WebsiteUrl is null || Https(o.WebsiteUrl), "Mcp:Server:WebsiteUrl"),
        (o.Icons is not null && o.Icons.All(i => i is not null && Https(i.Source)
            && ImageMime(i.MimeType)
            && i.Theme is null or "light" or "dark" && (i.Sizes is null || i.Sizes.All(s => s == "any" || Size(s)))), "Mcp:Server:Icons"));
    private static bool Size(string? value)
    {
        var parts = value?.Split('x');
        return parts?.Length == 2 && parts.All(p => p.Length > 0 && p.All(c => c is >= '0' and <= '9'))
            && int.TryParse(parts[0], out var w) && w > 0 && int.TryParse(parts[1], out var h) && h > 0;
    }
    private static bool ImageMime(string? value) => Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(value, out var mime)
        && mime.MediaType.Value?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        && !mime.MediaType.Value.Contains('*') && mime.Parameters.Count == 0;
    private sealed class ConfigurationNamespace { public ConfigurationNamespace() { } }
    private sealed class Validator<T>(Func<T, IEnumerable<string>> validate) : IValidateOptions<T> where T : class
    {
        public ValidateOptionsResult Validate(string? name, T options)
        {
            var errors = validate(options).ToArray();
            return errors.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}

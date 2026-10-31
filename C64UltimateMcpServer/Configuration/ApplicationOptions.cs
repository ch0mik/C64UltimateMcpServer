using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Configuration;

public sealed class DownloadOptions
{
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(100);
}
public sealed class ProgramArtifactOptions
{
    public int MaxEntries { get; set; } = 256;
    public long MaxBytes { get; set; } = 16 * 1024 * 1024;
    public int MaxArtifactBytes { get; set; } = 4 * 1024 * 1024;
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(1);
}
public sealed class McpTaskStoreOptions
{
    public int Capacity { get; set; } = 256;
    public TimeSpan TerminalRetention { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);
}
public sealed class ProgramWorkflowOptions
{
    public int Capacity { get; set; } = 256;
    public TimeSpan ContinuationLifetime { get; set; } = TimeSpan.FromHours(1);
    public int MaxRounds { get; set; } = 8;
    public int MaxSourceBytes { get; set; } = 1024 * 1024;
    public int MaxGenerationRequestBytes { get; set; } = 65536;
    public int SamplingMaxTokens { get; set; } = 4096;
    public int MaxRootHints { get; set; } = 20;
}
public sealed class LiveResourceOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int ListenerQueueCapacity { get; set; } = 32;
}
public sealed class ContentCacheOptions
{
    public TimeSpan DocumentationTimeToLive { get; set; } = TimeSpan.FromHours(1);
}
public sealed class McpAppsOptions
{
    public int PreviewBytes { get; set; } = 256;
}
public sealed class McpNotificationOptions
{
    public bool LegacyLoggingEnabled { get; set; } = true;
    public TimeSpan LegacyLoggingInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan CleanupTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
public sealed class McpTransportOptions
{
    public string EndpointPath { get; set; } = "/";
    public string HealthPath { get; set; } = "/health";
    // Keep a string so numeric enum configuration cannot silently select a mode.
    public string SessionMode { get; set; } = "StatefulForInitializeClients";
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(2);
    public int MaxIdleSessionCount { get; set; } = 10000;
}
public sealed class McpServerIdentityOptions
{
    public string Name { get; set; } = "C64 Ultimate MCP Server";
    public string Version { get; set; } = "1.0.0";
    public string? Title { get; set; } = "MCP Server for Commodore 64 Ultimate";
    public string? Description { get; set; } = "A comprehensive MCP server for the Commodore 64 Ultimate with 45+ tools for device control, file management, and machine operations";
    public string? WebsiteUrl { get; set; } = "https://ultimate64.com/";
    public Icon[] Icons { get; set; } = [];

    // Used only when the Icons key is absent; binding into an initialized array appends entries.
    public static Icon[] DefaultIcons() => [new Icon
    {
        Source = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg",
        MimeType = "image/svg+xml", Sizes = ["any"], Theme = "light"
    }];
}

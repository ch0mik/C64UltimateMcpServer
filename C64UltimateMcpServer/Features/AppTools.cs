using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace C64UltimateMcpServer.Features;

// MCP Apps remains experimental in SDK 2.3. Ordinary text/structured results work without UI support.
#pragma warning disable MCPEXP003
public sealed class AppTools(DeviceResourceHub live, ProgramArtifactStore artifacts, ProgramWorkflow workflow, IOptions<McpAppsOptions>? settings = null)
{
    private readonly int _previewBytes = (settings?.Value ?? new()).PreviewBytes;

    [McpServerTool(Name = "ultimate_dashboard", ReadOnly = true)]
    [McpAppUi(ResourceUri = "ui://c64/dashboard.html")]
    [Description("Read device availability, API version and drives. The UI can refresh through the MCP host; no device action is performed.")]
    public async Task<CallToolResult> Dashboard(CancellationToken cancellationToken) => FeatureProtocol.Result(new
    {
        status = await live.ReadAsync(DeviceResourceHub.StatusUri, cancellationToken),
        drives = await live.ReadAsync(DeviceResourceHub.DrivesUri, cancellationToken)
    });

    [McpServerTool(Name = "ultimate_program_preview", ReadOnly = true)]
    [McpAppUi(ResourceUri = "ui://c64/program-preview.html")]
    [Description("Inspect an artifact's metadata and at most 256 PRG bytes. This never runs the program.")]
    public CallToolResult Preview(string artifactId)
    {
        try
        {
            var item = artifacts.Get(artifactId);
            return FeatureProtocol.Result(new { artifact = item.Info, hex = Convert.ToHexString(item.Bytes.Take(_previewBytes).ToArray()), diagnostics = Array.Empty<string>() });
        }
        catch (ModelContextProtocol.McpProtocolException ex)
        { return FeatureProtocol.Result(new { errorType = "ArtifactUnavailable", error = ex.Message }, true); }
    }

    [McpServerTool(Name = "ultimate_request_program_execution")]
    [McpAppUi(ResourceUri = "ui://c64/program-preview.html", Visibility = [McpUiToolVisibility.App])]
    [Description("Prepare an execution-confirmation request for the host conversation. This tool never executes a device command.")]
    public async Task<CallToolResult> RequestExecution(McpServer server, string artifactId, CancellationToken cancellationToken)
    {
        if (server.ClientCapabilities?.Extensions?.ContainsKey("io.modelcontextprotocol/ui") != true)
            return FeatureProtocol.Result(new { errorType = "MissingCapability", error = "This helper requires the MCP Apps UI capability." }, true);
        var parameters = new CallToolRequestParams { Name = "ultimate_program_workflow", Arguments = new Dictionary<string, JsonElement>
        { ["artifactId"] = JsonSerializer.SerializeToElement(artifactId), ["execute"] = JsonSerializer.SerializeToElement(true) } };
        try { return await workflow.RunAsync(new(server.IsMrtrSupported, server.ClientCapabilities), parameters, (_, _) => { }, cancellationToken); }
        catch (InputRequiredException pending)
        {
            // Apps tool-call bridges expect ordinary CallToolResult. Hand the continuation to the host conversation.
            return FeatureProtocol.Result(JsonSerializer.SerializeToElement(pending.Result, ModelContextProtocol.McpJsonUtilities.DefaultOptions));
        }
    }

    [McpServerResource(UriTemplate = "ui://c64/dashboard.html", Name = "C64 dashboard UI", MimeType = "text/html;profile=mcp-app")]
    public TextResourceContents DashboardUi() => ReadUi("dashboard.html");

    [McpServerResource(UriTemplate = "ui://c64/program-preview.html", Name = "C64 program preview UI", MimeType = "text/html;profile=mcp-app")]
    public TextResourceContents PreviewUi() => ReadUi("program-preview.html");

    private static TextResourceContents ReadUi(string file) => new()
    {
        Uri = $"ui://c64/{file}", MimeType = "text/html;profile=mcp-app",
        Text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Features", "Ui", "dist", file)),
        Meta = new JsonObject { ["ui"] = new JsonObject
        { ["csp"] = new JsonObject { ["connectDomains"] = new JsonArray(), ["resourceDomains"] = new JsonArray() } } }
    };
}
#pragma warning restore MCPEXP003

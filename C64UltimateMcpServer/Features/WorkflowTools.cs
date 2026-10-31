using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Features;

public sealed class WorkflowTools(ProgramWorkflow workflow)
{
    [McpServerTool(Name = "ultimate_program_workflow")]
    [Description("Compile BASIC/assembly to a temporary PRG artifact. Missing input uses MRTR. execute=true requires explicit confirmation before resetting and running the C64. Supply source content, never a server filesystem path.")]
    public Task<CallToolResult> Program(McpServer server, RequestContext<CallToolRequestParams> context,
        IProgress<ProgressNotificationValue> progress, string? language = null, string? source = null,
        string? generationRequest = null, bool execute = false, bool selectProject = false, string? artifactId = null,
        CancellationToken cancellationToken = default) => workflow.RunAsync(new(server.IsMrtrSupported, server.ClientCapabilities), context.Params!,
            (phase, current) => progress.Report(new() { Progress = current, Total = 5, Message = phase }), cancellationToken);
}

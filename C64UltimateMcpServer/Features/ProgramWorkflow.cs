using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using C64AssemblyPrgGenerator;
using C64BasicPrgGenerator;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Features;

// Sampling/roots are deprecated standalone requests; here they are explicit MRTR input requests.
#pragma warning disable MCP9005
public sealed class ProgramWorkflow(ProgramArtifactStore artifacts, UltimateDeviceClientFactory clients, TimeProvider clock, IOptions<ProgramWorkflowOptions>? settings = null)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Operation> _pending = new();
    private readonly ProgramWorkflowOptions _options = settings?.Value ?? new();

    public sealed record Arguments(string? Language = null, string? Source = null, string? GenerationRequest = null,
        bool Execute = false, bool SelectProject = false, string? ArtifactId = null);

    private sealed class Operation(Arguments args, string hash, string target, DateTimeOffset expiry)
    {
        internal Arguments Args = args;
        internal readonly string Hash = hash;
        internal readonly string Target = target;
        internal DateTimeOffset ExpiresAt = expiry;
        internal string Stage = "parameters";
        internal ProgramArtifact? Artifact;
        internal bool RootsSelected;
        internal string? ProjectHint;
        internal int Rounds;
    }

    public async Task<CallToolResult> RunAsync(WorkflowClient client, CallToolRequestParams request,
        Action<string, float> report, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var connection = clients.GetOptions();
        var args = JsonSerializer.SerializeToElement(request.Arguments ?? new Dictionary<string, JsonElement>())
            .Deserialize<Arguments>(FeatureProtocol.Json) ?? new();
        if (Encoding.UTF8.GetByteCount(args.Source ?? "") > _options.MaxSourceBytes || Encoding.UTF8.GetByteCount(args.GenerationRequest ?? "") > _options.MaxGenerationRequestBytes)
            return Error("InputTooLarge", $"Source is limited to {_options.MaxSourceBytes} bytes and generation requests to {_options.MaxGenerationRequestBytes} bytes.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(args, FeatureProtocol.Json))));
        Operation op;
        try
        {
            if (request.RequestState is { } token)
            {
                lock (_gate)
                {
                    Sweep();
                    if (!_pending.TryGetValue(token, out op!) || op.Hash != fingerprint || op.Target != connection.BaseUrl)
                        return Error("InvalidContinuation", "Continuation expired, was replayed, or does not match its original arguments and target.");
                    // Consume before processing: concurrent retries cannot repeat a device effect.
                    _pending.Remove(token);
                }
                if (request.InputResponses?.TryGetValue(op.Stage, out var input) != true)
                    return Error("MissingInputResponse", $"Expected input response '{op.Stage}'. Start a new workflow.");
                if (op.Stage is "parameters" or "source" or "confirm")
                {
                    var answer = input!.Deserialize(InputResponse.ElicitResultJsonTypeInfo);
                    if (answer?.IsAccepted != true)
                        return FeatureProtocol.Result(new { success = true, executed = false, outcome = "declined", artifact = op.Artifact });
                    string? Text(string name) => answer.Content?.TryGetValue(name, out var value) == true && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                    if (op.Stage == "confirm")
                    {
                        if (answer.Content?.TryGetValue("confirm", out var yes) != true || yes.ValueKind != JsonValueKind.True)
                            return FeatureProtocol.Result(new { success = true, executed = false, outcome = "declined", artifact = op.Artifact });
                        var artifact = artifacts.Get(op.Artifact!.Id);
                        report("sending program", 4);
                        using var device = clients.Create(connection);
                        try { await device.RunProgramBinaryAsync(artifact.Bytes, ct); }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) when (ex is HttpRequestException or C64UltimateClient.UltimateClientException)
                        { return Error("ExecutionUnconfirmed", "The device did not confirm execution. The command will not be retried automatically."); }
                        report("completed", 5);
                        return FeatureProtocol.Result(new { success = true, executed = true, artifact = artifact.Info, target = op.Target });
                    }
                    op.Args = op.Args with
                    {
                        Language = string.IsNullOrWhiteSpace(op.Args.Language) ? Text("language") : op.Args.Language,
                        Source = string.IsNullOrWhiteSpace(op.Args.Source) ? Text("source") : op.Args.Source
                    };
                }
                else if (op.Stage == "sampling")
                {
                    var answer = input!.Deserialize(InputResponse.CreateMessageResultJsonTypeInfo);
                    var source = string.Join("\n", answer?.Content.OfType<TextContentBlock>().Select(t => t.Text) ?? []);
                    if (source.StartsWith("```", StringComparison.Ordinal))
                    {
                        var lines = source.Trim().Split('\n');
                        source = string.Join("\n", lines.Skip(1).Take(lines.Length - 2));
                    }
                    op.Args = op.Args with { Source = source };
                }
                else if (op.Stage == "roots")
                {
                    var roots = input!.Deserialize(InputResponse.ListRootsResultJsonTypeInfo);
                    op.ProjectHint = string.Join(", ", roots?.Roots.Take(_options.MaxRootHints).Select(r => r.Uri) ?? []);
                    op.RootsSelected = true;
                }
            }
            else op = new(args, fingerprint, connection.BaseUrl, clock.GetUtcNow() + _options.ContinuationLifetime);

            if (Encoding.UTF8.GetByteCount(op.Args.Source ?? "") > _options.MaxSourceBytes)
                return Error("InputTooLarge", $"Source exceeds {_options.MaxSourceBytes} bytes.");
            report("collecting parameters", 0);
            if (string.IsNullOrWhiteSpace(op.Args.Language) && op.Args.ArtifactId is null)
                Ask(client, op, "parameters", "Choose language (basic or assembly) and provide source if available.",
                    new() { ["language"] = new ElicitRequestParams.StringSchema() }, ["language"]);
            if (op.Args.Language is not null) op.Args = op.Args with { Language = op.Args.Language.Trim().ToLowerInvariant() };
            if (op.Args.Language is not (null or "basic" or "assembly")) return Error("InvalidInput", "language must be basic or assembly.");
            if (op.Args.SelectProject && !op.RootsSelected)
            {
                if (client.Capabilities?.Roots is null) return Error("MissingCapability", "Supply source directly; this client does not advertise roots.");
                Require(client, op, "roots", InputRequest.ForRootsList(new()));
            }
            if (string.IsNullOrWhiteSpace(op.Args.Source) && op.Args.ArtifactId is null)
            {
                if (!string.IsNullOrWhiteSpace(op.Args.GenerationRequest))
                {
                    if (client.Capabilities?.Sampling is null)
                        return Error("MissingCapability", "Supply source directly; this client does not advertise sampling.");
                    Require(client, op, "sampling", InputRequest.ForSampling(new CreateMessageRequestParams
                    {
                        MaxTokens = _options.SamplingMaxTokens,
                        SystemPrompt = $"Return only valid Commodore 64 {op.Args.Language} source for the built-in generator. No Markdown.",
                        Messages = [new SamplingMessage { Role = Role.User, Content = [new TextContentBlock { Text = op.Args.GenerationRequest }] }]
                    }));
                }
                Ask(client, op, "source", $"Provide {op.Args.Language} source text. Client project hints: {op.ProjectHint ?? "none"}. Paths are never opened on this server.",
                    new() { ["source"] = new ElicitRequestParams.StringSchema() }, ["source"]);
            }
            if (op.Artifact is null)
            {
                report("compiling", 2);
                if (op.Args.ArtifactId is { } id) op.Artifact = artifacts.Get(id).Info;
                else
                {
                    var source = op.Args.Source!;
                    if (Encoding.UTF8.GetByteCount(source) > _options.MaxSourceBytes) return Error("InputTooLarge", $"Source exceeds {_options.MaxSourceBytes} bytes.");
                    var bytes = op.Args.Language == "basic" ? new BasicPrgGenerator().GeneratePrg(source)
                        : new AssemblyPrgGenerator().GeneratePrg(source, true).PrgBytes;
                    op.Artifact = artifacts.Add(bytes);
                }
                op.ExpiresAt = op.ExpiresAt < op.Artifact.ExpiresAt ? op.ExpiresAt : op.Artifact.ExpiresAt;
            }
            if (op.Args.Execute)
            {
                report("waiting for execution confirmation", 3);
                Ask(client, op, "confirm", $"Run artifact {op.Artifact.Id}, SHA-256 {op.Artifact.Sha256}, on {op.Target}? This resets the C64 and runs the PRG.",
                    new() { ["confirm"] = new ElicitRequestParams.BooleanSchema() }, ["confirm"]);
            }
            report("completed", 5);
            return FeatureProtocol.Result(new { success = true, executed = false, artifact = op.Artifact, diagnostics = Array.Empty<string>() });
        }
        catch (InputRequiredException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (ArgumentException ex) { return Error("CompilationError", ex.Message); }
        catch (JsonException) { return Error("InvalidInputResponse", "The client input response did not match the requested schema."); }
        catch (McpProtocolException ex) { return Error("ArtifactUnavailable", ex.Message); }
        catch (WorkflowCapabilityException ex) { return Error("MissingCapability", ex.Message); }
    }

    private void Ask(WorkflowClient client, Operation op, string stage, string message,
        Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition> properties, string[] required)
    {
        if (client.Capabilities?.Elicitation?.Form is null)
            throw new WorkflowCapabilityException("This interaction requires form elicitation. Supply language and source explicitly; execution requires confirmation with a capable client.");
        Require(client, op, stage, InputRequest.ForElicitation(new ElicitRequestParams
        { Message = message, RequestedSchema = new() { Properties = properties, Required = required } }));
    }

    private void Require(WorkflowClient client, Operation op, string stage, InputRequest input)
    {
        if (!client.SupportsMrtr) throw new WorkflowCapabilityException("Client does not support MRTR. Supply language and source explicitly and omit execute/selectProject/generationRequest.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_gate)
        {
            Sweep();
            if (++op.Rounds > _options.MaxRounds) throw new WorkflowCapabilityException("Maximum interaction rounds exceeded. Start a new workflow with explicit arguments.");
            if (_pending.Count >= _options.Capacity) throw new WorkflowCapabilityException("Workflow capacity reached; retry after pending operations expire.");
            op.Stage = stage;
            _pending.Add(token, op);
        }
        throw new InputRequiredException(new Dictionary<string, InputRequest> { [stage] = input }, token);
    }

    private void Sweep()
    {
        foreach (var key in _pending.Where(p => p.Value.ExpiresAt <= clock.GetUtcNow()).Select(p => p.Key).ToArray()) _pending.Remove(key);
    }
    internal void Discard(string? state)
    { if (state is not null) lock (_gate) _pending.Remove(state); }
    private static CallToolResult Error(string type, string message) => FeatureProtocol.Result(new { errorType = type, error = message, retryable = false }, true);
    private sealed class WorkflowCapabilityException(string message) : Exception(message);
}
#pragma warning restore MCP9005

using System.Collections.Concurrent;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Features;

public sealed record WorkflowClient(bool SupportsMrtr, ClientCapabilities? Capabilities);

/// <summary>Composes the workflow's MRTR rounds with Tasks; SDK 2.3 does not do this for attributed tools.</summary>
public sealed class WorkflowTaskRunner(BoundedTaskStore store, ProgramWorkflow workflow, ILogger<WorkflowTaskRunner> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly ConcurrentDictionary<string, Task> _running = new();
    private readonly CancellationTokenSource _shutdown = new();

    public Task StartAsync(CancellationToken cancellationToken)
    { store.Cancelled += Cancel; return Task.CompletedTask; }

    public async Task<CreateTaskResult> LaunchAsync(WorkflowClient client, CallToolRequestParams request, CancellationToken ct)
    {
        var info = await store.CreateTaskAsync(ct);
        var copy = JsonSerializer.Deserialize<CallToolRequestParams>(JsonSerializer.Serialize(request, McpJsonUtilities.DefaultOptions), McpJsonUtilities.DefaultOptions)!;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        _cancellations[info.TaskId] = cancellation;
        var task = Task.Run(() => ExecuteAsync(info.TaskId, client, copy, cancellation), CancellationToken.None);
        _running[info.TaskId] = task;
        _ = task.ContinueWith(_ => _running.TryRemove(info.TaskId, out var removed), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return new() { TaskId = info.TaskId, Status = info.Status, CreatedAt = info.CreatedAt,
            LastUpdatedAt = info.LastUpdatedAt, PollIntervalMs = info.PollIntervalMs };
    }

    private async Task ExecuteAsync(string taskId, WorkflowClient client, CallToolRequestParams request, CancellationTokenSource cancellation)
    {
        try
        {
            while (true)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                try
                {
                    var result = await workflow.RunAsync(client, request, (phase, _) => store.SetPhase(taskId, phase), cancellation.Token);
                    await store.SetCompletedAsync(taskId, JsonSerializer.SerializeToElement(result, McpJsonUtilities.DefaultOptions));
                    break;
                }
                catch (InputRequiredException input)
                {
                    if (input.Result.InputRequests is not { Count: > 0 } requests)
                        throw new InvalidOperationException("Workflow must publish an input request before waiting.");
                    request.RequestState = input.Result.RequestState;
                    var answers = await store.AwaitInputsAsync(taskId, requests, cancellation.Token);
                    request.InputResponses = answers;
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { await store.SetCancelledAsync(taskId); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow task {TaskId} failed", taskId);
            await store.SetFailedAsync(taskId, JsonSerializer.SerializeToElement(new { code = -32603, message = "Workflow task failed." }));
        }
        finally
        {
            workflow.Discard(request.RequestState);
            _cancellations.TryRemove(taskId, out _);
            cancellation.Dispose();
        }
    }

    private void Cancel(string taskId)
    {
        if (_cancellations.TryGetValue(taskId, out var cancellation))
            try { cancellation.Cancel(); } catch (ObjectDisposedException) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        store.Cancelled -= Cancel;
        _shutdown.Cancel();
        await Task.WhenAll(_running.Values).WaitAsync(cancellationToken);
    }
}

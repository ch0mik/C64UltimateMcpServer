using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Features;

public sealed class BoundedTaskStore(TimeProvider clock, IOptions<McpTaskStoreOptions>? settings = null) : IMcpTaskStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, McpTaskInfo> _tasks = new();
    private readonly int Capacity = (settings?.Value ?? new()).Capacity;
    private readonly TimeSpan Retention = (settings?.Value ?? new()).TerminalRetention;
    private readonly TimeSpan PollInterval = (settings?.Value ?? new()).PollInterval;
    public event Action<InputResponseReceivedEventArgs>? InputResponseReceived;
    internal event Action<string>? Cancelled;

    public Task<McpTaskInfo> CreateTaskAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            Sweep();
            if (_tasks.Count >= Capacity)
                throw new McpProtocolException("TaskCapacityExceeded: all task slots are occupied. Retry after retention expiry.", McpErrorCode.InternalError);
            var now = clock.GetUtcNow();
            var info = new McpTaskInfo(Guid.NewGuid().ToString("N"), McpTaskStatus.Working, now, now,
                PollIntervalMs: (int)PollInterval.TotalMilliseconds, StatusMessage: "working");
            _tasks.Add(info.TaskId, info);
            return Task.FromResult(info);
        }
    }

    public Task<McpTaskInfo?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) { Sweep(); return Task.FromResult(_tasks.GetValueOrDefault(taskId)); }
    }

    public Task SetCompletedAsync(string taskId, JsonElement result, CancellationToken cancellationToken = default) =>
        Finish(taskId, McpTaskStatus.Completed, result.Clone(), null);
    public Task SetFailedAsync(string taskId, JsonElement error, CancellationToken cancellationToken = default) =>
        Finish(taskId, McpTaskStatus.Failed, null, error.Clone());

    private Task Finish(string taskId, McpTaskStatus state, JsonElement? result, JsonElement? error)
    {
        lock (_gate)
        {
            if (_tasks.TryGetValue(taskId, out var info) && !Terminal(info.Status))
                _tasks[taskId] = info with { Status = state, Result = result, Error = error,
                    LastUpdatedAt = clock.GetUtcNow(), StatusMessage = state.ToString().ToLowerInvariant(),
                    InputRequests = null, TimeToLive = clock.GetUtcNow() - info.CreatedAt + Retention };
        }
        return Task.CompletedTask;
    }

    public Task<bool> SetCancelledAsync(string taskId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_tasks.TryGetValue(taskId, out var info) || Terminal(info.Status)) return Task.FromResult(false);
            Finish(taskId, McpTaskStatus.Cancelled, null, null);
        }
        Cancelled?.Invoke(taskId);
        return Task.FromResult(true);
    }

    internal void SetPhase(string taskId, string phase)
    {
        lock (_gate)
            if (_tasks.TryGetValue(taskId, out var info) && !Terminal(info.Status))
                _tasks[taskId] = info with { StatusMessage = phase, LastUpdatedAt = clock.GetUtcNow() };
    }

    public Task SetInputRequestsAsync(string taskId, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_tasks.TryGetValue(taskId, out var info) || Terminal(info.Status)) return Task.CompletedTask;
            var pending = info.InputRequests?.ToDictionary(p => p.Key, p => p.Value) ?? new();
            foreach (var pair in inputRequests) pending[pair.Key] = pair.Value;
            _tasks[taskId] = info with { Status = McpTaskStatus.InputRequired, InputRequests = pending,
                StatusMessage = "waiting for client input", LastUpdatedAt = clock.GetUtcNow() };
        }
        return Task.CompletedTask;
    }

    public Task ResolveInputRequestsAsync(string taskId, IDictionary<string, InputResponse> inputResponses, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_tasks.TryGetValue(taskId, out var info) || Terminal(info.Status)) return Task.CompletedTask;
            var pending = info.InputRequests?.ToDictionary(p => p.Key, p => p.Value) ?? new();
            if (inputResponses.Keys.Any(key => !pending.ContainsKey(key)))
                throw new McpProtocolException("Unknown or already answered input request.", McpErrorCode.InvalidParams);
            foreach (var key in inputResponses.Keys) pending.Remove(key);
            _tasks[taskId] = info with { InputRequests = pending,
                Status = pending.Count == 0 ? McpTaskStatus.Working : McpTaskStatus.InputRequired,
                LastUpdatedAt = clock.GetUtcNow() };
        }
        foreach (var pair in inputResponses)
            InputResponseReceived?.Invoke(new() { TaskId = taskId, RequestId = pair.Key, Response = pair.Value });
        return Task.CompletedTask;
    }

    internal async Task<IDictionary<string, InputResponse>> AwaitInputsAsync(string taskId,
        IDictionary<string, InputRequest> requests, CancellationToken ct)
    {
        var received = new Dictionary<string, InputResponse>();
        var ready = new TaskCompletionSource<IDictionary<string, InputResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Receive(InputResponseReceivedEventArgs e)
        {
            if (e.TaskId != taskId || !requests.ContainsKey(e.RequestId)) return;
            lock (received)
            {
                received[e.RequestId] = e.Response;
                if (received.Count == requests.Count) ready.TrySetResult(new Dictionary<string, InputResponse>(received));
            }
        }
        InputResponseReceived += Receive;
        try { await SetInputRequestsAsync(taskId, requests, ct); return await ready.Task.WaitAsync(ct); }
        finally { InputResponseReceived -= Receive; }
    }

    private void Sweep()
    {
        foreach (var id in _tasks.Where(p => Terminal(p.Value.Status) && clock.GetUtcNow() - p.Value.LastUpdatedAt >= Retention)
                     .Select(p => p.Key).ToArray()) _tasks.Remove(id);
    }
    private static bool Terminal(McpTaskStatus state) => state is McpTaskStatus.Completed or McpTaskStatus.Failed or McpTaskStatus.Cancelled;
}

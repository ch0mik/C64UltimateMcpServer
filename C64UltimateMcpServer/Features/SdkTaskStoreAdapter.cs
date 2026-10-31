using System.Text.Json;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Features;

/// <summary>SDK 2.3 requires a store instance before DI is built. Attach the DI-owned store before mapping MCP.</summary>
internal sealed class SdkTaskStoreAdapter : IMcpTaskStore
{
    private IMcpTaskStore? _store;
    private IMcpTaskStore Store => _store ?? throw new InvalidOperationException("Task store has not been initialized.");
    internal void Initialize(IMcpTaskStore store)
    {
        if (Interlocked.CompareExchange(ref _store, store, null) is not null)
            throw new InvalidOperationException("Task store is already initialized.");
    }
    public event Action<InputResponseReceivedEventArgs>? InputResponseReceived
    { add => Store.InputResponseReceived += value; remove => Store.InputResponseReceived -= value; }
    public Task<McpTaskInfo> CreateTaskAsync(CancellationToken cancellationToken = default) => Store.CreateTaskAsync(cancellationToken);
    public Task<McpTaskInfo?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default) => Store.GetTaskAsync(taskId, cancellationToken);
    public Task SetCompletedAsync(string taskId, JsonElement result, CancellationToken cancellationToken = default) => Store.SetCompletedAsync(taskId, result, cancellationToken);
    public Task SetFailedAsync(string taskId, JsonElement error, CancellationToken cancellationToken = default) => Store.SetFailedAsync(taskId, error, cancellationToken);
    public Task<bool> SetCancelledAsync(string taskId, CancellationToken cancellationToken = default) => Store.SetCancelledAsync(taskId, cancellationToken);
    public Task SetInputRequestsAsync(string taskId, IDictionary<string, InputRequest> requests, CancellationToken cancellationToken = default) => Store.SetInputRequestsAsync(taskId, requests, cancellationToken);
    public Task ResolveInputRequestsAsync(string taskId, IDictionary<string, InputResponse> responses, CancellationToken cancellationToken = default) => Store.ResolveInputRequestsAsync(taskId, responses, cancellationToken);
}

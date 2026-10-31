using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using C64UltimateClient;

namespace C64UltimateMcpServer.Features;

public sealed record DeviceSnapshot(bool Available, DateTimeOffset ObservedAt, JsonElement? Data, string? Error);

/// <summary>One shared observer, with bounded queues and polling only for subscribed live URIs.</summary>
public sealed class DeviceResourceHub(UltimateDeviceClientFactory clients, TimeProvider clock, IOptions<LiveResourceOptions>? settings = null) : BackgroundService
{
    private readonly LiveResourceOptions _options = settings?.Value ?? new();
    public const string StatusUri = "c64://device/status";
    public const string DrivesUri = "c64://device/drives";
    public static bool IsLive(string uri) => uri is StatusUri or DrivesUri;
    private readonly object _gate = new();
    private readonly Dictionary<string, JsonNode?> _signatures = new();
    private readonly HashSet<Listener> _listeners = new();
    private readonly SemaphoreSlim _reads = new(1, 1);
    private CancellationTokenSource? _pollCancellation;
    private string? _pollUri;
    internal int ListenerCount { get { lock (_gate) return _listeners.Count; } }

    public Listener Watch(Func<IEnumerable<string>> interests)
    {
        var listener = new Listener(this, interests, _options.ListenerQueueCapacity);
        lock (_gate) _listeners.Add(listener);
        return listener;
    }

    public void InterestsChanged()
    {
        CancellationTokenSource? cancel = null;
        lock (_gate)
            if (_pollUri is { } uri && !_listeners.Any(l => l.Interests().Contains(uri)))
                cancel = _pollCancellation;
        try { cancel?.Cancel(); } catch (ObjectDisposedException) { }
    }

    public async Task<DeviceSnapshot> ReadAsync(string uri, CancellationToken ct)
    {
        if (!IsLive(uri)) throw new ArgumentException("Unknown live resource.");
        await _reads.WaitAsync(ct);
        try
        {
            DeviceSnapshot snapshot;
            try
            {
                using var client = clients.Create();
                var data = uri == StatusUri
                    ? JsonSerializer.SerializeToElement(await client.GetVersionAsync(ct))
                    : JsonSerializer.SerializeToElement(await client.GetDrivesInfoAsync(ct));
                snapshot = new(true, clock.GetUtcNow(), data, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or UltimateClientException or OperationCanceledException)
            { snapshot = new(false, clock.GetUtcNow(), null, ex.Message); }
            var signature = JsonSerializer.SerializeToNode(new { snapshot.Available, snapshot.Data, snapshot.Error });
            lock (_gate)
            {
                if (!_signatures.TryGetValue(uri, out var old) || !JsonNode.DeepEquals(old, signature))
                {
                    _signatures[uri] = signature;
                    foreach (var listener in _listeners)
                        if (listener.Interests().Contains(uri)) listener.Publish(uri);
                }
            }
            return snapshot;
        }
        finally { _reads.Release(); }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string[] uris;
            CancellationTokenSource poll;
            lock (_gate)
            {
                uris = _listeners.SelectMany(l => l.Interests()).Where(IsLive).Distinct().ToArray();
                _pollCancellation = poll = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            }
            using (poll)
            {
                try
                {
                    foreach (var uri in uris)
                    {
                        lock (_gate)
                        {
                            if (!_listeners.Any(l => l.Interests().Contains(uri))) continue;
                            _pollUri = uri;
                        }
                        await ReadAsync(uri, poll.Token);
                    }
                }
                catch (OperationCanceledException) when (poll.IsCancellationRequested) { }
                finally
                {
                    lock (_gate)
                        if (ReferenceEquals(_pollCancellation, poll)) { _pollCancellation = null; _pollUri = null; }
                }
            }
            await Task.Delay(_options.PollInterval, clock, stoppingToken);
        }
    }

    public sealed class Listener : IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _queued = new();
        private readonly DeviceResourceHub _owner;
        internal Func<IEnumerable<string>> Interests { get; }
        internal Channel<string> Queue { get; }
        internal Listener(DeviceResourceHub owner, Func<IEnumerable<string>> interests, int capacity)
        {
            _owner = owner; Interests = interests;
            Queue = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
                { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
        }
        public async IAsyncEnumerable<string> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var uri in Queue.Reader.ReadAllAsync(ct))
            { _queued.TryRemove(uri, out _); yield return uri; }
        }
        internal void Publish(string uri)
        {
            if (_queued.TryAdd(uri, 0) && !Queue.Writer.TryWrite(uri)) _queued.TryRemove(uri, out _);
        }
        public void Dispose()
        {
            lock (_owner._gate)
            {
                _owner._listeners.Remove(this);
            }
            _owner.InterestsChanged();
            Queue.Writer.TryComplete();
        }
    }
}

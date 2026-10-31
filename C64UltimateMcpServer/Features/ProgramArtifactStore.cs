using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using System.Security.Cryptography;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace C64UltimateMcpServer.Features;

public sealed record ProgramArtifact(string Id, string Sha256, int SizeBytes, int LoadAddress,
    DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)
{
    public string Uri => $"c64://artifacts/{Id}";
}

/// <summary>Process-local, bounded storage. Identifiers never refer to filesystem paths.</summary>
public sealed class ProgramArtifactStore(TimeProvider clock, IOptions<ProgramArtifactOptions>? settings = null)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (ProgramArtifact Info, byte[] Bytes)> _items = new();
    private readonly int MaxEntries = (settings?.Value ?? new()).MaxEntries;
    private readonly long MaxBytes = (settings?.Value ?? new()).MaxBytes;
    private readonly int MaxArtifactBytes = (settings?.Value ?? new()).MaxArtifactBytes;
    private readonly TimeSpan Lifetime = (settings?.Value ?? new()).Lifetime;

    public ProgramArtifact Add(byte[] bytes)
    {
        if (bytes.Length < 2 || bytes.Length > MaxArtifactBytes || bytes.Length > MaxBytes)
            throw new ArgumentException("PRG must contain a load address and fit the artifact size limit.");
        lock (_gate)
        {
            Sweep();
            while (_items.Count >= MaxEntries || _items.Values.Sum(v => (long)v.Bytes.Length) + bytes.Length > MaxBytes)
                _items.Remove(_items.MinBy(p => p.Value.Info.CreatedAt).Key);
            var now = clock.GetUtcNow();
            var info = new ProgramArtifact(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), bytes.Length,
                bytes[0] | (bytes[1] << 8), now, now + Lifetime);
            _items.Add(info.Id, (info, bytes.ToArray()));
            return info;
        }
    }

    public (ProgramArtifact Info, byte[] Bytes) Get(string id)
    {
        lock (_gate)
        {
            Sweep();
            if (!_items.TryGetValue(id, out var item))
                throw new McpProtocolException("ArtifactUnavailable: the artifact is unknown, expired, or evicted.", McpErrorCode.InvalidParams);
            return (item.Info, item.Bytes.ToArray());
        }
    }

    private void Sweep()
    {
        foreach (var id in _items.Where(p => p.Value.Info.ExpiresAt <= clock.GetUtcNow()).Select(p => p.Key).ToArray())
            _items.Remove(id);
    }
}

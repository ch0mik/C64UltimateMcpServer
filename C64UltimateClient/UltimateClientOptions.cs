using System;

namespace C64UltimateClient;

/// <summary>Device connection settings. NetworkPassword is never part of an MCP tool schema.</summary>
public sealed class UltimateClientOptions
{
    /// <summary>Absolute address of the device REST service.</summary>
    public string BaseUrl { get; set; } = "http://192.168.0.120";
    /// <summary>Optional device network password sent only as an X-Password header.</summary>
    public string? NetworkPassword { get; set; }
    /// <summary>Device HTTP timeout, unless the constructor supplies an explicit timeout.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

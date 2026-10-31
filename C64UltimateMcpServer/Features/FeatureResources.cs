using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C64UltimateMcpServer.Features;

[McpServerResourceType]
public sealed class FeatureResources(ProgramArtifactStore artifacts, DeviceResourceHub live)
{
    [McpServerResource(UriTemplate = "c64://device/status", Name = "Ultimate device status", MimeType = "application/json")]
    [Description("Current availability and REST API version; does not infer CPU state.")]
    public Task<TextResourceContents> Status(CancellationToken cancellationToken) => ReadLive(DeviceResourceHub.StatusUri, cancellationToken);

    [McpServerResource(UriTemplate = "c64://device/drives", Name = "Ultimate drive status", MimeType = "application/json")]
    [Description("Current drive data or an explicit unavailable observation.")]
    public Task<TextResourceContents> Drives(CancellationToken cancellationToken) => ReadLive(DeviceResourceHub.DrivesUri, cancellationToken);

    private async Task<TextResourceContents> ReadLive(string uri, CancellationToken ct) => new()
    {
        Uri = uri, MimeType = "application/json",
        Text = JsonSerializer.Serialize(await live.ReadAsync(uri, ct), new JsonSerializerOptions(JsonSerializerDefaults.Web))
    };

    [McpServerResource(UriTemplate = "c64://artifacts/{id}", Name = "Compiled PRG artifact", MimeType = "application/octet-stream")]
    [Description("Read an unexpired PRG artifact. IDs are process-local and are not file paths.")]
    public BlobResourceContents Artifact(string id)
    {
        var item = artifacts.Get(id);
        return BlobResourceContents.FromBytes(item.Bytes, item.Info.Uri, "application/octet-stream");
    }
}

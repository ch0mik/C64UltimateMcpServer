using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace C64UltimateClient;

/// <summary>
/// Client exception for API errors and other operation failures.
/// </summary>
public class UltimateClientException : Exception
{
    public IReadOnlyList<string>? ApiErrors { get; set; }

    public UltimateClientException(string message, IReadOnlyList<string>? apiErrors = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ApiErrors = apiErrors;
    }
}

/// <summary>
/// Complete .NET client for Commodore 64 Ultimate REST API.
/// Implements all endpoints from 1541U documentation: https://1541u-documentation.readthedocs.io/
/// </summary>
public class Ultimate64Client : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Ultimate64Client>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public Ultimate64Client(string baseUrl, ILogger<Ultimate64Client>? logger = null, TimeSpan? timeout = null)
    {
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _logger?.LogInformation($"[Ultimate64Client] Initialized with BaseUrl: {baseUrl}");
    }

    #region About

    /// <summary>
    /// GET /v1/version - Get API version
    /// </summary>
    public async Task<VersionResponse> GetVersionAsync()
    {
        _logger?.LogInformation("GetVersionAsync");
        try
        {
            var response = await _httpClient.GetAsync("/v1/version");
            return await HandleResponse<VersionResponse>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get version");
            throw;
        }
    }

    #endregion

    #region Machine Control

    /// <summary>
    /// PUT /v1/machine:reset - Soft reset (configuration preserved)
    /// </summary>
    public async Task ResetMachineAsync()
    {
        _logger?.LogInformation("ResetMachineAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:reset", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset machine");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:reboot - Full reboot (reinitialize cartridge)
    /// </summary>
    public async Task RebootMachineAsync()
    {
        _logger?.LogInformation("RebootMachineAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:reboot", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reboot machine");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:pause - Pause CPU by pulling DMA line low
    /// </summary>
    public async Task PauseMachineAsync()
    {
        _logger?.LogInformation("PauseMachineAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:pause", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pause machine");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:resume - Resume CPU from paused state
    /// </summary>
    public async Task ResumeMachineAsync()
    {
        _logger?.LogInformation("ResumeMachineAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:resume", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to resume machine");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:poweroff - Power off (U64 only, may not return response)
    /// </summary>
    public async Task PowerOffMachineAsync()
    {
        _logger?.LogInformation("PowerOffMachineAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:poweroff", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to power off machine");
            throw;
        }
    }

    #endregion

    #region Memory Operations

    /// <summary>
    /// GET /v1/machine:readmem - Read from C64 memory via DMA
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "D000")</param>
    /// <param name="length">Number of bytes to read (default 256, max 65536)</param>
    public async Task<byte[]> ReadMemoryAsync(string address, int length = 256)
    {
        _logger?.LogInformation($"ReadMemoryAsync address={address} length={length}");
        try
        {
            var url = $"/v1/machine:readmem?address={address}&length={length}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                throw new UltimateClientException($"Failed to read memory: HTTP {response.StatusCode}");

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read memory");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:writemem - Write to C64 memory via DMA (query string method, max 128 bytes)
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "D020")</param>
    /// <param name="data">Bytes to write in hex format (e.g., "0504")</param>
    public async Task WriteMemoryAsync(string address, string data)
    {
        _logger?.LogInformation($"WriteMemoryAsync address={address} data={data}");
        try
        {
            var url = $"/v1/machine:writemem?address={address}&data={data}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write memory");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/machine:writemem - Write to C64 memory via DMA (binary method, supports >128 bytes)
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "0800")</param>
    /// <param name="data">Binary data to write</param>
    public async Task WriteMemoryBinaryAsync(string address, byte[] data)
    {
        _logger?.LogInformation($"WriteMemoryBinaryAsync address={address} size={data.Length}");
        try
        {
            var url = $"/v1/machine:writemem?address={address}";
            var content = new ByteArrayContent(data)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync(url, content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write memory binary");
            throw;
        }
    }

    /// <summary>
    /// GET /v1/machine:debugreg - Read debug register $D7FF (U64 only)
    /// </summary>
    public async Task<string> GetDebugRegisterAsync()
    {
        _logger?.LogInformation("GetDebugRegisterAsync");
        try
        {
            var response = await _httpClient.GetAsync("/v1/machine:debugreg");
            var result = await HandleResponse<JsonElement>(response);
            return result.GetProperty("value").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get debug register");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:debugreg - Write debug register $D7FF (U64 only)
    /// </summary>
    /// <param name="value">Value in hex format (e.g., "FF")</param>
    public async Task<string> SetDebugRegisterAsync(string value)
    {
        _logger?.LogInformation($"SetDebugRegisterAsync value={value}");
        try
        {
            var url = $"/v1/machine:debugreg?value={value}";
            var response = await _httpClient.PutAsync(url, null);
            var result = await HandleResponse<JsonElement>(response);
            return result.GetProperty("value").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set debug register");
            throw;
        }
    }

    #endregion

    #region Audio Playback

    /// <summary>
    /// PUT /v1/runners:sidplay - Play SID file from filesystem
    /// </summary>
    public async Task PlaySidAsync(string file, int? songNumber = null)
    {
        _logger?.LogInformation($"PlaySidAsync file={file} song={songNumber}");
        try
        {
            var queryParams = new List<string> { $"file={Uri.EscapeDataString(file)}" };
            if (songNumber.HasValue)
                queryParams.Add($"songnr={songNumber.Value}");

            var url = $"/v1/runners:sidplay?{string.Join("&", queryParams)}";
            _logger?.LogDebug($"PlaySidAsync URL: {url}");
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play SID");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/runners:sidplay - Play SID file from binary data
    /// </summary>
    public async Task PlaySidBinaryAsync(byte[] sidData, int? songNumber = null)
    {
        _logger?.LogInformation($"PlaySidBinaryAsync size={sidData.Length} songNumber={songNumber}");
        try
        {
            var content = new ByteArrayContent(sidData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var url = "/v1/runners:sidplay";
            if (songNumber.HasValue)
                url += $"?songnr={songNumber.Value}";

            _logger?.LogDebug($"PlaySidBinaryAsync URL: {url} size={sidData.Length}");
            var response = await _httpClient.PostAsync(url, content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play SID binary");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/runners:modplay - Play MOD file from filesystem
    /// </summary>
    public async Task PlayModAsync(string file)
    {
        _logger?.LogInformation($"PlayModAsync file={file}");
        try
        {
            var url = $"/v1/runners:modplay?file={Uri.EscapeDataString(file)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play MOD");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/runners:modplay - Play MOD file from binary data
    /// </summary>
    public async Task PlayModBinaryAsync(byte[] modData)
    {
        _logger?.LogInformation($"PlayModBinaryAsync size={modData.Length}");
        try
        {
            var content = new ByteArrayContent(modData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync("/v1/runners:modplay", content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play MOD binary");
            throw;
        }
    }

    #endregion

    #region Program & Cartridge Execution

    /// <summary>
    /// PUT /v1/runners:load_prg - Load PRG file from filesystem (does not run)
    /// </summary>
    public async Task LoadProgramAsync(string file)
    {
        _logger?.LogInformation($"LoadProgramAsync file={file}");
        try
        {
            var url = $"/v1/runners:load_prg?file={Uri.EscapeDataString(file)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load program");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/runners:run_prg - Run PRG file from filesystem
    /// </summary>
    public async Task RunProgramAsync(string file)
    {
        _logger?.LogInformation($"RunProgramAsync file={file}");
        try
        {
            var url = $"/v1/runners:run_prg?file={Uri.EscapeDataString(file)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run program");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/runners:run_prg - Run PRG file from binary data
    /// </summary>
    public async Task RunProgramBinaryAsync(byte[] prgData)
    {
        _logger?.LogInformation($"RunProgramBinaryAsync size={prgData.Length}");
        try
        {
            var content = new ByteArrayContent(prgData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync("/v1/runners:run_prg", content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run program binary");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/runners:run_crt - Run cartridge from filesystem
    /// </summary>
    public async Task RunCartridgeAsync(string file)
    {
        _logger?.LogInformation($"RunCartridgeAsync file={file}");
        try
        {
            var url = $"/v1/runners:run_crt?file={Uri.EscapeDataString(file)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run cartridge");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/runners:run_crt - Run cartridge from binary data
    /// </summary>
    public async Task RunCartridgeBinaryAsync(byte[] crtData)
    {
        _logger?.LogInformation($"RunCartridgeBinaryAsync size={crtData.Length}");
        try
        {
            var content = new ByteArrayContent(crtData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync("/v1/runners:run_crt", content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run cartridge binary");
            throw;
        }
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// GET /v1/configs - Get all configuration categories
    /// </summary>
    public async Task<ConfigCategoriesResponse> GetConfigCategoriesAsync()
    {
        _logger?.LogInformation("GetConfigCategoriesAsync");
        try
        {
            var response = await _httpClient.GetAsync("/v1/configs");
            return await HandleResponse<ConfigCategoriesResponse>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config categories");
            throw;
        }
    }

    /// <summary>
    /// GET /v1/configs/{category} - Get configuration items in a category (supports wildcards)
    /// </summary>
    public async Task<JsonElement> GetConfigCategoryAsync(string category)
    {
        _logger?.LogInformation($"GetConfigCategoryAsync category={category}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}";
            var response = await _httpClient.GetAsync(url);
            return await HandleResponse<JsonElement>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config category");
            throw;
        }
    }

    /// <summary>
    /// GET /v1/configs/{category}/{item} - Get specific configuration item (supports wildcards)
    /// </summary>
    public async Task<JsonElement> GetConfigItemAsync(string category, string item)
    {
        _logger?.LogInformation($"GetConfigItemAsync category={category} item={item}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(item)}";
            var response = await _httpClient.GetAsync(url);
            return await HandleResponse<JsonElement>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config item");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/configs/{category}/{item}?value={value} - Set configuration item
    /// </summary>
    public async Task SetConfigItemAsync(string category, string item, string value)
    {
        _logger?.LogInformation($"SetConfigItemAsync category={category} item={item} value={value}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(item)}?value={Uri.EscapeDataString(value)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set config item");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/configs - Bulk update multiple configuration items
    /// </summary>
    public async Task BulkConfigUpdateAsync(JsonElement config)
    {
        _logger?.LogInformation("BulkConfigUpdateAsync");
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(config, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync("/v1/configs", content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to bulk update config");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/configs:save_to_flash - Save current configuration to non-volatile memory
    /// </summary>
    public async Task SaveConfigAsync()
    {
        _logger?.LogInformation("SaveConfigAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/configs:save_to_flash", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save config");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/configs:load_from_flash - Load configuration from non-volatile memory
    /// </summary>
    public async Task LoadConfigAsync()
    {
        _logger?.LogInformation("LoadConfigAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/configs:load_from_flash", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load config");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/configs:reset_to_default - Reset configuration to factory defaults (does not clear saved values)
    /// </summary>
    public async Task ResetConfigToDefaultAsync()
    {
        _logger?.LogInformation("ResetConfigToDefaultAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/configs:reset_to_default", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset config to default");
            throw;
        }
    }

    #endregion

    #region Drive Management

    /// <summary>
    /// GET /v1/drives - Get information about all drives
    /// </summary>
    public async Task<DrivesResponse> GetDrivesInfoAsync()
    {
        _logger?.LogInformation("GetDrivesInfoAsync");
        try
        {
            var response = await _httpClient.GetAsync("/v1/drives");
            return await HandleResponse<DrivesResponse>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get drives info");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:mount - Mount disk image from filesystem
    /// </summary>
    /// <param name="drive">Drive identifier: "a", "b", "softiec", etc.</param>
    /// <param name="image">File path to disk image</param>
    /// <param name="type">Image type: "d64", "g64", "d71", "g71", "d81" (optional, auto-detected)</param>
    /// <param name="mode">"readwrite", "readonly", or "unlinked" (optional)</param>
    public async Task MountDiskAsync(string drive, string image, string? type = null, string? mode = null)
    {
        _logger?.LogInformation($"MountDiskAsync drive={drive} image={image} type={type} mode={mode}");
        try
        {
            var queryParams = new List<string> { $"image={Uri.EscapeDataString(image)}" };
            if (!string.IsNullOrEmpty(type))
                queryParams.Add($"type={type}");
            if (!string.IsNullOrEmpty(mode))
                queryParams.Add($"mode={mode}");

            var url = $"/v1/drives/{drive}:mount?{string.Join("&", queryParams)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mount disk");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/drives/{drive}:mount - Mount disk image from binary data
    /// </summary>
    public async Task MountDiskBinaryAsync(string drive, byte[] diskData, string? type = null, string? mode = null)
    {
        _logger?.LogInformation($"MountDiskBinaryAsync drive={drive} size={diskData.Length} type={type} mode={mode}");
        try
        {
            var url = $"/v1/drives/{drive}:mount";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(type))
                queryParams.Add($"type={type}");
            if (!string.IsNullOrEmpty(mode))
                queryParams.Add($"mode={mode}");
            
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var content = new ByteArrayContent(diskData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync(url, content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mount disk binary");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:unmount or :remove - Unmount disk image
    /// </summary>
    public async Task UnmountDiskAsync(string drive)
    {
        _logger?.LogInformation($"UnmountDiskAsync drive={drive}");
        try
        {
            var response = await _httpClient.PutAsync($"/v1/drives/{drive}:remove", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unmount disk");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:reset - Reset drive
    /// </summary>
    public async Task ResetDriveAsync(string drive)
    {
        _logger?.LogInformation($"ResetDriveAsync drive={drive}");
        try
        {
            var response = await _httpClient.PutAsync($"/v1/drives/{drive}:reset", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset drive");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:set_mode - Change drive mode/type
    /// </summary>
    /// <param name="drive">Drive identifier</param>
    /// <param name="mode">"1541", "1571", or "1581"</param>
    public async Task SetDriveModeAsync(string drive, string mode)
    {
        _logger?.LogInformation($"SetDriveModeAsync drive={drive} mode={mode}");
        try
        {
            var url = $"/v1/drives/{drive}:set_mode?mode={mode}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set drive mode");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:on - Enable drive
    /// </summary>
    public async Task EnableDriveAsync(string drive)
    {
        _logger?.LogInformation($"EnableDriveAsync drive={drive}");
        try
        {
            var response = await _httpClient.PutAsync($"/v1/drives/{drive}:on", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enable drive");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:off - Disable drive
    /// </summary>
    public async Task DisableDriveAsync(string drive)
    {
        _logger?.LogInformation($"DisableDriveAsync drive={drive}");
        try
        {
            var response = await _httpClient.PutAsync($"/v1/drives/{drive}:off", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to disable drive");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/drives/{drive}:load_rom - Load custom drive ROM from filesystem
    /// </summary>
    public async Task LoadDriveRomAsync(string drive, string file)
    {
        _logger?.LogInformation($"LoadDriveRomAsync drive={drive} file={file}");
        try
        {
            var url = $"/v1/drives/{drive}:load_rom?file={Uri.EscapeDataString(file)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load drive ROM");
            throw;
        }
    }

    /// <summary>
    /// POST /v1/drives/{drive}:load_rom - Load custom drive ROM from binary data
    /// </summary>
    public async Task LoadDriveRomBinaryAsync(string drive, byte[] romData)
    {
        _logger?.LogInformation($"LoadDriveRomBinaryAsync drive={drive} size={romData.Length}");
        try
        {
            var content = new ByteArrayContent(romData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            var response = await _httpClient.PostAsync($"/v1/drives/{drive}:load_rom", content);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load drive ROM binary");
            throw;
        }
    }

    #endregion

    #region Data Streams (U64 only)

    /// <summary>
    /// PUT /v1/streams/{stream}:start - Start video/audio/debug stream
    /// </summary>
    /// <param name="streamName">"video", "audio", or "debug"</param>
    /// <param name="ipAddress">IP address to send stream to</param>
    /// <param name="port">Port number (default: 11000 for video, 11001 for audio, 11002 for debug)</param>
    public async Task StartStreamAsync(string streamName, string ipAddress, int? port = null)
    {
        _logger?.LogInformation($"StartStreamAsync stream={streamName} ip={ipAddress} port={port}");
        try
        {
            var target = port.HasValue ? $"{ipAddress}:{port}" : ipAddress;
            var url = $"/v1/streams/{streamName}:start?ip={Uri.EscapeDataString(target)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start stream");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/streams/{stream}:stop - Stop video/audio/debug stream
    /// </summary>
    public async Task StopStreamAsync(string streamName)
    {
        _logger?.LogInformation($"StopStreamAsync stream={streamName}");
        try
        {
            var response = await _httpClient.PutAsync($"/v1/streams/{streamName}:stop", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop stream");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/machine:menu_button - Press the menu button
    /// Enters or exits the Ultimate menu depending on current state
    /// </summary>
    public async Task MenuButtonAsync()
    {
        _logger?.LogInformation("MenuButtonAsync");
        try
        {
            var response = await _httpClient.PutAsync("/v1/machine:menu_button", null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to press menu button");
            throw;
        }
    }

    #endregion

    #region File Operations

    /// <summary>
    /// GET /v1/files/{path}:info - Get file information
    /// </summary>
    public async Task<JsonElement> GetFileInfoAsync(string path)
    {
        _logger?.LogInformation($"GetFileInfoAsync path={path}");
        try
        {
            var url = $"/v1/files/{Uri.EscapeDataString(path)}:info";
            var response = await _httpClient.GetAsync(url);
            return await HandleResponse<JsonElement>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get file info");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/files/{path}:create_d64 - Create D64 disk image
    /// </summary>
    /// <param name="path">Full file path for new D64</param>
    /// <param name="tracks">Number of tracks: 35 (default) or 40</param>
    /// <param name="diskName">Name for disk header (optional, defaults to filename)</param>
    public async Task CreateD64Async(string path, int tracks = 35, string? diskName = null)
    {
        _logger?.LogInformation($"CreateD64Async path={path} tracks={tracks} diskName={diskName}");
        try
        {
            var queryParams = new List<string> { $"tracks={tracks}" };
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_d64?{string.Join("&", queryParams)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D64");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/files/{path}:create_d71 - Create D71 disk image (70 tracks)
    /// </summary>
    public async Task CreateD71Async(string path, string? diskName = null)
    {
        _logger?.LogInformation($"CreateD71Async path={path} diskName={diskName}");
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_d71";
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D71");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/files/{path}:create_d81 - Create D81 disk image (160 tracks, 80 per side)
    /// </summary>
    public async Task CreateD81Async(string path, string? diskName = null)
    {
        _logger?.LogInformation($"CreateD81Async path={path} diskName={diskName}");
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_d81";
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D81");
            throw;
        }
    }

    /// <summary>
    /// PUT /v1/files/{path}:create_dnp - Create DNP disk image
    /// </summary>
    /// <param name="path">Full file path for new DNP</param>
    /// <param name="tracks">Number of tracks (required, each track = 256 sectors, max 255)</param>
    /// <param name="diskName">Name for disk header (optional)</param>
    public async Task CreateDnpAsync(string path, int tracks, string? diskName = null)
    {
        _logger?.LogInformation($"CreateDnpAsync path={path} tracks={tracks} diskName={diskName}");
        try
        {
            var queryParams = new List<string> { $"tracks={tracks}" };
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_dnp?{string.Join("&", queryParams)}";
            var response = await _httpClient.PutAsync(url, null);
            await HandleResponse(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create DNP");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger?.LogError($"HTTP {response.StatusCode}: {error}");
            throw new UltimateClientException($"API error: HTTP {response.StatusCode}", null);
        }

        var content = await response.Content.ReadAsStringAsync();
        
        // Try to parse JSON response
        try
        {
            var doc = JsonDocument.Parse(content);
            
            // Check for errors in response
            if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && 
                errorsElement.ValueKind == JsonValueKind.Array)
            {
                var errors = errorsElement.EnumerateArray()
                    .Select(e => e.GetString() ?? "Unknown error")
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                if (errors.Count > 0)
                {
                    _logger?.LogWarning($"API returned errors: {string.Join(", ", errors)}");
                    throw new UltimateClientException("API returned errors", errors);
                }
            }

            return JsonSerializer.Deserialize<T>(content, _jsonOptions) 
                ?? throw new UltimateClientException("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            // If not JSON, assume binary response or simple text
            if (typeof(T) == typeof(string))
                return (T)(object)content;
            
            _logger?.LogError(ex, "Failed to parse JSON response");
            throw new UltimateClientException("Invalid JSON response", null, ex);
        }
    }

    private async Task HandleResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger?.LogError($"HTTP {response.StatusCode}: {error}");
            throw new UltimateClientException($"API error: HTTP {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        // Parse response for errors
        if (!string.IsNullOrEmpty(content))
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                
                if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && 
                    errorsElement.ValueKind == JsonValueKind.Array)
                {
                    var errors = errorsElement.EnumerateArray()
                        .Select(e => e.GetString() ?? "Unknown error")
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();

                    if (errors.Count > 0)
                    {
                        _logger?.LogWarning($"API returned errors: {string.Join(", ", errors)}");
                        throw new UltimateClientException("API returned errors", errors);
                    }
                }
            }
            catch (JsonException)
            {
                // Response is not JSON, just log it
                _logger?.LogInformation($"Response: {content}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient?.Dispose();
        _disposed = true;
    }

    #endregion
}

#region Response Models

/// <summary>API version response</summary>
public class VersionResponse
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>Configuration categories response</summary>
public class ConfigCategoriesResponse
{
    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>Drive information</summary>
public class DriveInfo
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("bus_id")]
    public int BusId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("rom")]
    public string? Rom { get; set; }

    [JsonPropertyName("image_file")]
    public string? ImageFile { get; set; }

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; set; }
}

/// <summary>Drives information response</summary>
public class DrivesResponse
{
    [JsonPropertyName("drives")]
    public List<JsonElement> Drives { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

#endregion

using C64UltimateClient;
using C64BasicPrgGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C64UltimateMcpServer.Core;

/// <summary>
/// MCP Server wrapper around C64UltimateClient.
/// Exposes Commodore 64 Ultimate REST API as MCP tools.
/// </summary>
public class UltimateService
{
    private readonly Ultimate64Client _client;
    private readonly ILogger<UltimateService> _serviceLogger;
    private readonly ILogger<Ultimate64Client> _clientLogger;

    public UltimateService(ILogger<UltimateService> serviceLogger, ILogger<Ultimate64Client> clientLogger, IConfiguration configuration)
    {
        _serviceLogger = serviceLogger;
        _clientLogger = clientLogger;
        var baseUrl = configuration["Ultimate:BaseUrl"] ?? "http://192.168.0.120";
        _client = new Ultimate64Client(baseUrl, _clientLogger);
        _serviceLogger.LogInformation($"[UltimateService] Initialized with C64UltimateClient pointing to: {baseUrl}");
    }

    // Connection Management

    [McpServerTool(Name = "ultimate_set_connection")]
    public async Task<string> SetConnection(string hostname, int? port = null)
    {
        _serviceLogger.LogInformation($"Setting connection to {hostname}:{port}");
        // Note: This is a metadata operation not in the official API
        return JsonSerializer.Serialize(new { ok = true, message = $"Connection managed via client initialization" });
    }

    [McpServerTool(Name = "ultimate_get_connection")]
    public async Task<string> GetConnection()
    {
        _serviceLogger.LogInformation("Getting current connection details");
        try
        {
            var version = await _client.GetVersionAsync();
            return JsonSerializer.Serialize(new { version = version.Version, connected = true });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to verify connection");
            return JsonSerializer.Serialize(new { error = ex.Message, connected = false });
        }
    }

    [McpServerTool(Name = "ultimate_version")]
    public async Task<string> GetVersion()
    {
        _serviceLogger.LogInformation("Getting API version");
        try
        {
            var version = await _client.GetVersionAsync();
            return JsonSerializer.Serialize(new { version = version.Version, errors = version.Errors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get version");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Audio Playback

    [McpServerTool(Name = "ultimate_play_sid")]
    public async Task<string> PlaySid(string file, int? songNumber = null)
    {
        _serviceLogger.LogInformation($"Playing SID file: {file}, song: {songNumber}");
        try
        {
            await _client.PlaySidAsync(file, songNumber);
            return JsonSerializer.Serialize(new { ok = true, message = $"Playing {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID file");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID file");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_play_sid_binary")]
    public async Task<string> PlaySidBinary(string? filePath = null, string? sidDataBase64 = null, string? url = null, int? songNumber = null)
    {
        _serviceLogger.LogInformation($"Playing SID binary (filePath={filePath}, base64={sidDataBase64?.Length ?? 0}, url={url}, song={songNumber})");
        try
        {
            byte[]? sidData = null;
            string sourceInfo = "";

            if (!string.IsNullOrEmpty(sidDataBase64))
            {
                try
                {
                    sidData = Convert.FromBase64String(sidDataBase64.Trim());
                    sourceInfo = $"base64 data ({sidData.Length} bytes)";
                    _serviceLogger.LogInformation($"Decoded base64: {sidData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid base64 data: {ex.Message}" });
                }
            }
            else if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    _serviceLogger.LogInformation($"Downloading SID from URL: {url}");
                    using var httpClient = new HttpClient();
                    var downloadResponse = await httpClient.GetAsync(url);
                    sidData = await downloadResponse.Content.ReadAsByteArrayAsync();
                    sourceInfo = $"URL ({sidData.Length} bytes)";
                    _serviceLogger.LogInformation($"Downloaded: {sidData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Failed to download from URL: {ex.Message}" });
                }
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (!File.Exists(filePath))
                        return JsonSerializer.Serialize(new { error = $"File not found: {filePath}" });
                    
                    sidData = await File.ReadAllBytesAsync(filePath);
                    sourceInfo = $"file {filePath} ({sidData.Length} bytes)";
                    _serviceLogger.LogInformation($"Read file: {sidData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Failed to read file: {ex.Message}" });
                }
            }
            else
            {
                return JsonSerializer.Serialize(new { error = "One of filePath, sidDataBase64, or url must be provided" });
            }

            if (sidData == null || sidData.Length == 0)
                return JsonSerializer.Serialize(new { error = "No SID data provided or data is empty" });

            if (sidData.Length < 4)
                return JsonSerializer.Serialize(new { error = "SID file is too small (must be at least 4 bytes for header)" });

            await _client.PlaySidBinaryAsync(sidData, songNumber);
            
            return JsonSerializer.Serialize(new 
            { 
                success = true, 
                message = $"Playing SID from {sourceInfo}",
                size_bytes = sidData.Length
            });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID binary");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID binary");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_play_mod")]
    public async Task<string> PlayMod(string file)
    {
        _serviceLogger.LogInformation($"Playing MOD file: {file}");
        try
        {
            await _client.PlayModAsync(file);
            return JsonSerializer.Serialize(new { ok = true, message = $"Playing {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play MOD file");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play MOD file");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Program Execution

    [McpServerTool(Name = "ultimate_load_program")]
    public async Task<string> LoadProgram(string file)
    {
        _serviceLogger.LogInformation($"Loading program: {file}");
        try
        {
            await _client.LoadProgramAsync(file);
            return JsonSerializer.Serialize(new { ok = true, message = $"Loaded {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load program");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load program");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_run_program")]
    public async Task<string> RunProgram(string file)
    {
        _serviceLogger.LogInformation($"Running program: {file}");
        try
        {
            await _client.RunProgramAsync(file);
            return JsonSerializer.Serialize(new { ok = true, message = $"Running {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run program");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run program");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_run_prg_binary")]
    public async Task<string> RunPrgBinary(string? filePath = null, string? prgDataBase64 = null, string? url = null)
    {
        _serviceLogger.LogInformation($"Running PRG binary (filePath={filePath}, base64={prgDataBase64?.Length ?? 0}, url={url})");
        try
        {
            byte[]? prgData = null;
            string sourceInfo = "";

            if (!string.IsNullOrEmpty(prgDataBase64))
            {
                try
                {
                    prgData = Convert.FromBase64String(prgDataBase64.Trim());
                    sourceInfo = $"base64 data ({prgData.Length} bytes)";
                    _serviceLogger.LogInformation($"Decoded base64: {prgData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid base64 data: {ex.Message}" });
                }
            }
            else if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    _serviceLogger.LogInformation($"Downloading PRG from URL: {url}");
                    using var httpClient = new HttpClient();
                    var downloadResponse = await httpClient.GetAsync(url);
                    prgData = await downloadResponse.Content.ReadAsByteArrayAsync();
                    sourceInfo = $"URL ({prgData.Length} bytes)";
                    _serviceLogger.LogInformation($"Downloaded: {prgData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Failed to download from URL: {ex.Message}" });
                }
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (!File.Exists(filePath))
                        return JsonSerializer.Serialize(new { error = $"File not found: {filePath}" });
                    
                    prgData = await File.ReadAllBytesAsync(filePath);
                    sourceInfo = $"file {filePath} ({prgData.Length} bytes)";
                    _serviceLogger.LogInformation($"Read file: {prgData.Length} bytes");
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { error = $"Failed to read file: {ex.Message}" });
                }
            }
            else
            {
                return JsonSerializer.Serialize(new { error = "One of filePath, prgDataBase64, or url must be provided" });
            }

            if (prgData == null || prgData.Length == 0)
                return JsonSerializer.Serialize(new { error = "No PRG data provided or data is empty" });

            if (prgData.Length < 2)
                return JsonSerializer.Serialize(new { error = "PRG file is too small (must be at least 2 bytes)" });

            await _client.RunProgramBinaryAsync(prgData);
            
            return JsonSerializer.Serialize(new 
            { 
                success = true, 
                message = $"Running PRG from {sourceInfo}",
                size_bytes = prgData.Length
            });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run PRG binary");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run PRG binary");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_run_cartridge")]
    public async Task<string> RunCartridge(string file)
    {
        _serviceLogger.LogInformation($"Running cartridge: {file}");
        try
        {
            await _client.RunCartridgeAsync(file);
            return JsonSerializer.Serialize(new { ok = true, message = $"Running {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run cartridge");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run cartridge");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Configuration Management

    [McpServerTool(Name = "ultimate_get_config_categories")]
    public async Task<string> GetConfigCategories()
    {
        _serviceLogger.LogInformation("Getting config categories");
        try
        {
            var result = await _client.GetConfigCategoriesAsync();
            return JsonSerializer.Serialize(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config categories");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config categories");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_config_category")]
    public async Task<string> GetConfigCategory(string category)
    {
        _serviceLogger.LogInformation($"Getting config category: {category}");
        try
        {
            var result = await _client.GetConfigCategoryAsync(category);
            return JsonSerializer.Serialize(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config category");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config category");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_config_item")]
    public async Task<string> GetConfigItem(string category, string item)
    {
        _serviceLogger.LogInformation($"Getting config item: {category}/{item}");
        try
        {
            var result = await _client.GetConfigItemAsync(category, item);
            return JsonSerializer.Serialize(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config item");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config item");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_config_item")]
    public async Task<string> SetConfigItem(string category, string item, string value)
    {
        _serviceLogger.LogInformation($"Setting config item: {category}/{item} = {value}");
        try
        {
            await _client.SetConfigItemAsync(category, item, value);
            return JsonSerializer.Serialize(new { ok = true, message = "Config item set" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set config item");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set config item");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_bulk_config_update")]
    public async Task<string> BulkConfigUpdate(JsonElement config)
    {
        _serviceLogger.LogInformation("Bulk updating config");
        try
        {
            await _client.BulkConfigUpdateAsync(config);
            return JsonSerializer.Serialize(new { ok = true, message = "Config updated" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to bulk update config");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to bulk update config");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_save_config")]
    public async Task<string> SaveConfig()
    {
        _serviceLogger.LogInformation("Saving config to flash");
        try
        {
            await _client.SaveConfigAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Config saved" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to save config");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to save config");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_load_config")]
    public async Task<string> LoadConfig()
    {
        _serviceLogger.LogInformation("Loading config from flash");
        try
        {
            await _client.LoadConfigAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Config loaded" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load config");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load config");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reset_config")]
    public async Task<string> ResetConfig()
    {
        _serviceLogger.LogInformation("Resetting config to factory defaults");
        try
        {
            await _client.ResetConfigToDefaultAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Config reset to defaults" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset config");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset config");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Drive Operations

    [McpServerTool(Name = "ultimate_get_drives")]
    public async Task<string> GetDrivesInfo()
    {
        _serviceLogger.LogInformation("Getting drives information");
        try
        {
            var result = await _client.GetDrivesInfoAsync();
            return JsonSerializer.Serialize(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get drives info");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get drives info");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_mount_disk")]
    public async Task<string> MountDisk(string drive, string file, string? type = null, string? mode = null)
    {
        _serviceLogger.LogInformation($"Mounting disk {drive}: {file}");
        try
        {
            await _client.MountDiskAsync(drive, file, type, mode);
            return JsonSerializer.Serialize(new { ok = true, message = $"Disk mounted on {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to mount disk");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to mount disk");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_unmount_disk")]
    public async Task<string> UnmountDisk(string drive)
    {
        _serviceLogger.LogInformation($"Unmounting disk {drive}");
        try
        {
            await _client.UnmountDiskAsync(drive);
            return JsonSerializer.Serialize(new { ok = true, message = $"Disk unmounted from {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to unmount disk");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to unmount disk");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reset_drive")]
    public async Task<string> ResetDrive(string drive)
    {
        _serviceLogger.LogInformation($"Resetting drive {drive}");
        try
        {
            await _client.ResetDriveAsync(drive);
            return JsonSerializer.Serialize(new { ok = true, message = $"Drive {drive} reset" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset drive");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset drive");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_turn_drive_on")]
    public async Task<string> TurnDriveOn(string drive)
    {
        _serviceLogger.LogInformation($"Turning drive {drive} on");
        try
        {
            await _client.EnableDriveAsync(drive);
            return JsonSerializer.Serialize(new { ok = true, message = $"Drive {drive} enabled" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive on");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive on");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_turn_drive_off")]
    public async Task<string> TurnDriveOff(string drive)
    {
        _serviceLogger.LogInformation($"Turning drive {drive} off");
        try
        {
            await _client.DisableDriveAsync(drive);
            return JsonSerializer.Serialize(new { ok = true, message = $"Drive {drive} disabled" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive off");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive off");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_drive_mode")]
    public async Task<string> SetDriveMode(string drive, string mode)
    {
        _serviceLogger.LogInformation($"Setting drive {drive} mode to {mode}");
        try
        {
            await _client.SetDriveModeAsync(drive, mode);
            return JsonSerializer.Serialize(new { ok = true, message = $"Drive {drive} mode set to {mode}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set drive mode");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set drive mode");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_load_drive_rom")]
    public async Task<string> LoadDriveRom(string drive, string file)
    {
        _serviceLogger.LogInformation($"Loading ROM for drive {drive}: {file}");
        try
        {
            await _client.LoadDriveRomAsync(drive, file);
            return JsonSerializer.Serialize(new { ok = true, message = $"ROM loaded for drive {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load drive ROM");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load drive ROM");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Disk Image Creation

    [McpServerTool(Name = "ultimate_create_d64")]
    public async Task<string> CreateD64(string path, int? tracks = null, string? diskname = null)
    {
        _serviceLogger.LogInformation($"Creating D64 image: {path}");
        try
        {
            await _client.CreateD64Async(path, tracks ?? 35, diskname);
            return JsonSerializer.Serialize(new { ok = true, message = "D64 created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D64");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D64");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_create_d71")]
    public async Task<string> CreateD71(string path, string? diskname = null)
    {
        _serviceLogger.LogInformation($"Creating D71 image: {path}");
        try
        {
            await _client.CreateD71Async(path, diskname);
            return JsonSerializer.Serialize(new { ok = true, message = "D71 created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D71");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D71");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_create_d81")]
    public async Task<string> CreateD81(string path, string? diskname = null)
    {
        _serviceLogger.LogInformation($"Creating D81 image: {path}");
        try
        {
            await _client.CreateD81Async(path, diskname);
            return JsonSerializer.Serialize(new { ok = true, message = "D81 created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D81");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D81");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_create_dnp")]
    public async Task<string> CreateDnp(string path, int tracks, string? diskname = null)
    {
        _serviceLogger.LogInformation($"Creating DNP image: {path}");
        try
        {
            await _client.CreateDnpAsync(path, tracks, diskname);
            return JsonSerializer.Serialize(new { ok = true, message = "DNP created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create DNP");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create DNP");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // File Operations

    [McpServerTool(Name = "ultimate_get_file_info")]
    public async Task<string> GetFileInfo(string path)
    {
        _serviceLogger.LogInformation($"Getting file info: {path}");
        try
        {
            var result = await _client.GetFileInfoAsync(path);
            return JsonSerializer.Serialize(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get file info");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get file info");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Memory Operations

    [McpServerTool(Name = "ultimate_read_memory")]
    public async Task<string> ReadMemory(string address, int length = 256)
    {
        _serviceLogger.LogInformation($"Reading memory from {address}, length: {length}");
        try
        {
            var memory = await _client.ReadMemoryAsync(address, length);
            return JsonSerializer.Serialize(new { address, length, data = Convert.ToBase64String(memory) });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to read memory");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to read memory");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_write_memory")]
    public async Task<string> WriteMemory(string address, string data)
    {
        _serviceLogger.LogInformation($"Writing memory at {address}");
        try
        {
            await _client.WriteMemoryAsync(address, data);
            return JsonSerializer.Serialize(new { ok = true, message = "Memory written" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to write memory");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to write memory");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_write_memory_binary")]
    public async Task<string> WriteMemoryBinary(string address, byte[] data)
    {
        _serviceLogger.LogInformation($"Writing binary data to memory at {address}");
        try
        {
            await _client.WriteMemoryBinaryAsync(address, data);
            return JsonSerializer.Serialize(new { ok = true, message = $"Successfully wrote {data.Length} bytes to address {address}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to write binary data");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to write binary data");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_debug_register")]
    public async Task<string> GetDebugRegister()
    {
        _serviceLogger.LogInformation("Getting debug register");
        try
        {
            var value = await _client.GetDebugRegisterAsync();
            return JsonSerializer.Serialize(new { value });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get debug register");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get debug register");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_debug_register")]
    public async Task<string> SetDebugRegister(string value)
    {
        _serviceLogger.LogInformation($"Setting debug register to {value}");
        try
        {
            var result = await _client.SetDebugRegisterAsync(value);
            return JsonSerializer.Serialize(new { value = result });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set debug register");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set debug register");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Machine Control

    [McpServerTool(Name = "ultimate_reset_machine")]
    public async Task<string> ResetMachine()
    {
        _serviceLogger.LogInformation("Resetting machine");
        try
        {
            await _client.ResetMachineAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Machine reset" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset machine");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset machine");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reboot_device")]
    public async Task<string> RebootDevice()
    {
        _serviceLogger.LogInformation("Rebooting device");
        try
        {
            await _client.RebootMachineAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Device rebooting" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reboot device");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reboot device");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_pause_machine")]
    public async Task<string> PauseMachine()
    {
        _serviceLogger.LogInformation("Pausing machine");
        try
        {
            await _client.PauseMachineAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Machine paused" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to pause machine");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to pause machine");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_resume_machine")]
    public async Task<string> ResumeMachine()
    {
        _serviceLogger.LogInformation("Resuming machine");
        try
        {
            await _client.ResumeMachineAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Machine resumed" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to resume machine");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to resume machine");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_power_off")]
    public async Task<string> PowerOff()
    {
        _serviceLogger.LogInformation("Powering off device");
        try
        {
            await _client.PowerOffMachineAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Powering off" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to power off");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to power off");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Streaming

    [McpServerTool(Name = "ultimate_start_stream")]
    public async Task<string> StartStream(string stream, string ip, int? port = null)
    {
        _serviceLogger.LogInformation($"Starting stream {stream} to {ip}:{port}");
        try
        {
            await _client.StartStreamAsync(stream, ip, port);
            return JsonSerializer.Serialize(new { ok = true, message = $"Stream {stream} started" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to start stream");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to start stream");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_stop_stream")]
    public async Task<string> StopStream(string stream)
    {
        _serviceLogger.LogInformation($"Stopping stream {stream}");
        try
        {
            await _client.StopStreamAsync(stream);
            return JsonSerializer.Serialize(new { ok = true, message = $"Stream {stream} stopped" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to stop stream");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to stop stream");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_menu_button")]
    public async Task<string> MenuButton()
    {
        _serviceLogger.LogInformation("Pressing menu button");
        try
        {
            await _client.MenuButtonAsync();
            return JsonSerializer.Serialize(new { ok = true, message = "Menu button pressed" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to press menu button");
            return JsonSerializer.Serialize(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to press menu button");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // BASIC Program Generation

    [McpServerTool(Name = "ultimate_generate_basic_prg")]
    public Task<string> GenerateBasicPrg(string basicSource)
    {
        _serviceLogger.LogInformation("Generating BASIC PRG from source code");
        try
        {
            var generator = new BasicPrgGenerator();
            byte[] prgBytes = generator.GeneratePrg(basicSource);
            string base64Prg = Convert.ToBase64String(prgBytes);
            
            _serviceLogger.LogInformation($"Generated PRG ({prgBytes.Length} bytes) from BASIC source ({basicSource.Length} chars)");
            
            return Task.FromResult(JsonSerializer.Serialize(new 
            { 
                success = true,
                prgDataBase64 = base64Prg,
                sizeBytes = prgBytes.Length,
                loadAddress = "0x0801",
                note = "Use ultimate_run_prg_binary with prgDataBase64 parameter to execute"
            }));
        }
        catch (ArgumentException ex)
        {
            _serviceLogger.LogWarning(ex, "BASIC syntax error");
            return Task.FromResult(JsonSerializer.Serialize(new 
            { 
                success = false, 
                error = ex.Message,
                errorType = "SyntaxError"
            }));
        }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to generate BASIC PRG");
            return Task.FromResult(JsonSerializer.Serialize(new 
            { 
                success = false, 
                error = ex.Message,
                errorType = "GenerationError"
            }));
        }
    }
}


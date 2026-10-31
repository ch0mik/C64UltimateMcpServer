using Microsoft.Extensions.Options;
using C64UltimateMcpServer.Configuration;
using C64UltimateClient;
using C64UltimateMcpServer.Features;
using C64BasicPrgGenerator;
using C64AssemblyPrgGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C64UltimateMcpServer.Core;

/// <summary>
/// MCP Server wrapper around C64UltimateClient.
/// Exposes Commodore 64 Ultimate REST API as MCP tools.
/// Uses explicit tool result serialization; asynchronous execution is registered through the MCP Tasks extension.
/// </summary>
public class UltimateService : IDisposable
{
    private readonly Ultimate64Client _client;
    private readonly ILogger<UltimateService> _serviceLogger;
    private readonly ILogger<Ultimate64Client> _clientLogger;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly DownloadClientFactory _downloads;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public UltimateService(ILogger<UltimateService> serviceLogger, ILogger<Ultimate64Client> clientLogger,
        UltimateDeviceClientFactory clientFactory, DownloadClientFactory downloads)
    {
        _serviceLogger = serviceLogger;
        _clientLogger = clientLogger;
        _client = clientFactory.Create();
        _downloads = downloads;

        // Initialize JSON serializer options with camelCase and null handling
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
        
        _serviceLogger.LogInformation("[UltimateService] Initialized with validated device configuration");
    }

    // Retain the previous public signature without a second raw configuration path.
    public UltimateService(ILogger<UltimateService> serviceLogger, ILogger<Ultimate64Client> clientLogger,
        IConfiguration configuration, UltimateDeviceClientFactory? clientFactory = null)
        : this(serviceLogger, clientLogger, clientFactory ?? new UltimateDeviceClientFactory(
            LegacyConfigurationAdapter.Ultimate(configuration), new ClientLoggerFactory(clientLogger),
            () => new SocketsHttpHandler { AllowAutoRedirect = false }),
            new DownloadClientFactory(LegacyConfigurationAdapter.Downloads(configuration))) { }

    private sealed class ClientLoggerFactory(ILogger<Ultimate64Client> logger) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => logger;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    /// <summary>
    /// Helper method to serialize responses with custom JsonSerializerOptions.
    /// Ensures consistent JSON formatting across all tool responses.
    /// </summary>
    private string SerializeResponse(object obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to serialize response");
            // Use default options for error fallback to avoid recursion
            return JsonSerializer.Serialize(new { error = "Failed to serialize response", details = ex.Message });
        }
    }

    private CallToolResult CreateToolResult(object payload, bool isError = false)
    {
        return new CallToolResult
        {
            IsError = isError,
            StructuredContent = JsonSerializer.SerializeToElement(payload, _jsonOptions),
            Content =
            [
                new TextContentBlock
                {
                    Text = SerializeResponse(payload)
                }
            ]
        };
    }

    private CallToolResult CreateToolErrorResult(
        string message,
        string errorType,
        bool retryable = false,
        object? details = null)
    {
        return CreateToolResult(new
        {
            error = message,
            errorType,
            retryable,
            details
        }, isError: true);
    }

    // Connection Management

    [McpServerTool(Name = "ultimate_set_connection")]
    public async Task<string> SetConnection(string hostname, int? port = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Setting connection to {hostname}:{port}");
        // Note: This is a metadata operation not in the official API
        return SerializeResponse(new { ok = true, message = $"Connection managed via client initialization" });
    }

    [McpServerTool(Name = "ultimate_get_connection")]
    public async Task<string> GetConnection(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Getting current connection details");
        try
        {
            var version = await _client.GetVersionAsync(cancellationToken);
            return SerializeResponse(new { version = version.Version, connected = true });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to verify connection");
            return SerializeResponse(new { error = ex.Message, connected = false });
        }
    }

    [McpServerTool(Name = "ultimate_version")]
    public async Task<string> GetVersion(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Getting API version");
        try
        {
            var version = await _client.GetVersionAsync(cancellationToken);
            return SerializeResponse(new { version = version.Version, errors = version.Errors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get version");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Audio Playback

    [McpServerTool(Name = "ultimate_play_sid")]
    public async Task<string> PlaySid(string file, int? songNumber = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Playing SID file: {file}, song: {songNumber}");
        try
        {
            await _client.PlaySidAsync(file, songNumber, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Playing {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID file");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID file");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_play_sid_binary")]
    public async Task<CallToolResult> PlaySidBinary(string? filePath = null, string? sidDataBase64 = null, string? url = null, int? songNumber = null, CancellationToken cancellationToken = default)
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
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Invalid base64 data: {ex.Message}", "InvalidInput", details: new { parameter = nameof(sidDataBase64) });
                }
            }
            else if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    _serviceLogger.LogInformation($"Downloading SID from URL: {url}");
                    using var httpClient = _downloads.Create();
                    using var downloadResponse = await httpClient.GetAsync(url, cancellationToken);
                    sidData = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                    sourceInfo = $"URL ({sidData.Length} bytes)";
                    _serviceLogger.LogInformation($"Downloaded: {sidData.Length} bytes");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Failed to download from URL: {ex.Message}", "DownloadFailed", retryable: true, details: new { url });
                }
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (!File.Exists(filePath))
                        return CreateToolErrorResult($"File not found: {filePath}", "FileNotFound", details: new { filePath });
                    
                    sidData = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    sourceInfo = $"file {filePath} ({sidData.Length} bytes)";
                    _serviceLogger.LogInformation($"Read file: {sidData.Length} bytes");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Failed to read file: {ex.Message}", "FileReadFailed", details: new { filePath });
                }
            }
            else
            {
                return CreateToolErrorResult("One of filePath, sidDataBase64, or url must be provided", "MissingInput");
            }

            if (sidData == null || sidData.Length == 0)
                return CreateToolErrorResult("No SID data provided or data is empty", "InvalidInput");

            if (sidData.Length < 4)
                return CreateToolErrorResult("SID file is too small (must be at least 4 bytes for header)", "InvalidInput");

            await _client.PlaySidBinaryAsync(sidData, songNumber, cancellationToken);
            
            return CreateToolResult(new 
            { 
                success = true, 
                message = $"Playing SID from {sourceInfo}",
                size_bytes = sidData.Length
            });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID binary");
            return CreateToolErrorResult(ex.Message, "UltimateApiError", retryable: true, details: ex.ApiErrors);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play SID binary");
            return CreateToolErrorResult(ex.Message, "ExecutionError", retryable: true);
        }
    }

    [McpServerTool(Name = "ultimate_play_mod")]
    public async Task<string> PlayMod(string file, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Playing MOD file: {file}");
        try
        {
            await _client.PlayModAsync(file, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Playing {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to play MOD file");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to play MOD file");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Program Execution

    [McpServerTool(Name = "ultimate_load_program")]
    public async Task<string> LoadProgram(string file, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Loading program: {file}");
        try
        {
            await _client.LoadProgramAsync(file, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Loaded {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load program");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load program");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_run_program")]
    public async Task<string> RunProgram(string file, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Running program: {file}");
        try
        {
            await _client.RunProgramAsync(file, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Running {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run program");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run program");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_run_prg_binary")]
    public async Task<CallToolResult> RunPrgBinary(string? filePath = null, string? prgDataBase64 = null, string? url = null, CancellationToken cancellationToken = default)
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
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Invalid base64 data: {ex.Message}", "InvalidInput", details: new { parameter = nameof(prgDataBase64) });
                }
            }
            else if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    _serviceLogger.LogInformation($"Downloading PRG from URL: {url}");
                    using var httpClient = _downloads.Create();
                    using var downloadResponse = await httpClient.GetAsync(url, cancellationToken);
                    prgData = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                    sourceInfo = $"URL ({prgData.Length} bytes)";
                    _serviceLogger.LogInformation($"Downloaded: {prgData.Length} bytes");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Failed to download from URL: {ex.Message}", "DownloadFailed", retryable: true, details: new { url });
                }
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (!File.Exists(filePath))
                        return CreateToolErrorResult($"File not found: {filePath}", "FileNotFound", details: new { filePath });
                    
                    prgData = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    sourceInfo = $"file {filePath} ({prgData.Length} bytes)";
                    _serviceLogger.LogInformation($"Read file: {prgData.Length} bytes");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return CreateToolErrorResult($"Failed to read file: {ex.Message}", "FileReadFailed", details: new { filePath });
                }
            }
            else
            {
                return CreateToolErrorResult("One of filePath, prgDataBase64, or url must be provided", "MissingInput");
            }

            if (prgData == null || prgData.Length == 0)
                return CreateToolErrorResult("No PRG data provided or data is empty", "InvalidInput");

            if (prgData.Length < 2)
                return CreateToolErrorResult("PRG file is too small (must be at least 2 bytes)", "InvalidInput");

            await _client.RunProgramBinaryAsync(prgData, cancellationToken);
            
            return CreateToolResult(new 
            { 
                success = true, 
                message = $"Running PRG from {sourceInfo}",
                size_bytes = prgData.Length
            });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run PRG binary");
            return CreateToolErrorResult(ex.Message, "UltimateApiError", retryable: true, details: ex.ApiErrors);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run PRG binary");
            return CreateToolErrorResult(ex.Message, "ExecutionError", retryable: true);
        }
    }

    [McpServerTool(Name = "ultimate_run_cartridge")]
    public async Task<string> RunCartridge(string file, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Running cartridge: {file}");
        try
        {
            await _client.RunCartridgeAsync(file, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Running {file}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to run cartridge");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to run cartridge");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Configuration Management

    [McpServerTool(Name = "ultimate_get_config_categories")]
    public async Task<string> GetConfigCategories(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Getting config categories");
        try
        {
            var result = await _client.GetConfigCategoriesAsync(cancellationToken);
            return SerializeResponse(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config categories");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config categories");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_config_category")]
    public async Task<string> GetConfigCategory(string category, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Getting config category: {category}");
        try
        {
            var result = await _client.GetConfigCategoryAsync(category, cancellationToken);
            return SerializeResponse(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config category");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config category");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_config_item")]
    public async Task<string> GetConfigItem(string category, string item, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Getting config item: {category}/{item}");
        try
        {
            var result = await _client.GetConfigItemAsync(category, item, cancellationToken);
            return SerializeResponse(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config item");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get config item");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_config_item")]
    public async Task<string> SetConfigItem(string category, string item, string value, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Setting config item: {category}/{item} = {value}");
        try
        {
            await _client.SetConfigItemAsync(category, item, value, cancellationToken);
            return SerializeResponse(new { ok = true, message = "Config item set" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set config item");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set config item");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_bulk_config_update")]
    public async Task<string> BulkConfigUpdate(JsonElement config, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Bulk updating config");
        try
        {
            await _client.BulkConfigUpdateAsync(config, cancellationToken);
            return SerializeResponse(new { ok = true, message = "Config updated" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to bulk update config");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to bulk update config");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_save_config")]
    public async Task<string> SaveConfig(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Saving config to flash");
        try
        {
            await _client.SaveConfigAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Config saved" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to save config");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to save config");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_load_config")]
    public async Task<string> LoadConfig(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Loading config from flash");
        try
        {
            await _client.LoadConfigAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Config loaded" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load config");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load config");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reset_config")]
    public async Task<string> ResetConfig(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Resetting config to factory defaults");
        try
        {
            await _client.ResetConfigToDefaultAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Config reset to defaults" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset config");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset config");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Drive Operations

    [McpServerTool(Name = "ultimate_get_drives")]
    public async Task<string> GetDrivesInfo(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Getting drives information");
        try
        {
            var result = await _client.GetDrivesInfoAsync(cancellationToken);
            return SerializeResponse(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get drives info");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get drives info");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_mount_disk")]
    public async Task<string> MountDisk(string drive, string file, string? type = null, string? mode = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Mounting disk {drive}: {file}");
        try
        {
            await _client.MountDiskAsync(drive, file, type, mode, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Disk mounted on {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to mount disk");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to mount disk");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_unmount_disk")]
    public async Task<string> UnmountDisk(string drive, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Unmounting disk {drive}");
        try
        {
            await _client.UnmountDiskAsync(drive, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Disk unmounted from {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to unmount disk");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to unmount disk");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reset_drive")]
    public async Task<string> ResetDrive(string drive, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Resetting drive {drive}");
        try
        {
            await _client.ResetDriveAsync(drive, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Drive {drive} reset" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset drive");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset drive");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_turn_drive_on")]
    public async Task<string> TurnDriveOn(string drive, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Turning drive {drive} on");
        try
        {
            await _client.EnableDriveAsync(drive, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Drive {drive} enabled" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive on");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive on");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_turn_drive_off")]
    public async Task<string> TurnDriveOff(string drive, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Turning drive {drive} off");
        try
        {
            await _client.DisableDriveAsync(drive, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Drive {drive} disabled" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive off");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to turn drive off");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_drive_mode")]
    public async Task<string> SetDriveMode(string drive, string mode, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Setting drive {drive} mode to {mode}");
        try
        {
            await _client.SetDriveModeAsync(drive, mode, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Drive {drive} mode set to {mode}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set drive mode");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set drive mode");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_load_drive_rom")]
    public async Task<string> LoadDriveRom(string drive, string file, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Loading ROM for drive {drive}: {file}");
        try
        {
            await _client.LoadDriveRomAsync(drive, file, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"ROM loaded for drive {drive}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to load drive ROM");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to load drive ROM");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Disk Image Creation

    [McpServerTool(Name = "ultimate_create_d64")]
    public async Task<CallToolResult> CreateD64(string path, int? tracks = null, string? diskname = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Creating D64 image: {path}");
        try
        {
            await _client.CreateD64Async(path, tracks ?? 35, diskname, cancellationToken);
            return CreateToolResult(new { ok = true, message = "D64 created", path, tracks = tracks ?? 35, diskname });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D64");
            return CreateToolErrorResult(ex.Message, "UltimateApiError", retryable: true, details: ex.ApiErrors);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D64");
            return CreateToolErrorResult(ex.Message, "ExecutionError", retryable: true);
        }
    }

    [McpServerTool(Name = "ultimate_create_d71")]
    public async Task<string> CreateD71(string path, string? diskname = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Creating D71 image: {path}");
        try
        {
            await _client.CreateD71Async(path, diskname, cancellationToken);
            return SerializeResponse(new { ok = true, message = "D71 created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D71");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D71");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_create_d81")]
    public async Task<string> CreateD81(string path, string? diskname = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Creating D81 image: {path}");
        try
        {
            await _client.CreateD81Async(path, diskname, cancellationToken);
            return SerializeResponse(new { ok = true, message = "D81 created" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D81");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create D81");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_create_dnp")]
    public async Task<CallToolResult> CreateDnp(string path, int tracks, string? diskname = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Creating DNP image: {path}");
        try
        {
            await _client.CreateDnpAsync(path, tracks, diskname, cancellationToken);
            return CreateToolResult(new { ok = true, message = "DNP created", path, tracks, diskname });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to create DNP");
            return CreateToolErrorResult(ex.Message, "UltimateApiError", retryable: true, details: ex.ApiErrors);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to create DNP");
            return CreateToolErrorResult(ex.Message, "ExecutionError", retryable: true);
        }
    }

    // File Operations

    [McpServerTool(Name = "ultimate_get_file_info")]
    public async Task<string> GetFileInfo(string path, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Getting file info: {path}");
        try
        {
            var result = await _client.GetFileInfoAsync(path, cancellationToken);
            return SerializeResponse(result);
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get file info");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get file info");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Memory Operations

    [McpServerTool(Name = "ultimate_read_memory")]
    public async Task<string> ReadMemory(string address, int length = 256, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Reading memory from {address}, length: {length}");
        try
        {
            var memory = await _client.ReadMemoryAsync(address, length, cancellationToken);
            return SerializeResponse(new { address, length, data = Convert.ToBase64String(memory) });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to read memory");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to read memory");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_write_memory")]
    public async Task<string> WriteMemory(string address, string data, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Writing memory at {address}");
        try
        {
            await _client.WriteMemoryAsync(address, data, cancellationToken);
            return SerializeResponse(new { ok = true, message = "Memory written" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to write memory");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to write memory");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_write_memory_binary")]
    public async Task<string> WriteMemoryBinary(string address, byte[] data, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Writing binary data to memory at {address}");
        try
        {
            await _client.WriteMemoryBinaryAsync(address, data, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Successfully wrote {data.Length} bytes to address {address}" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to write binary data");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to write binary data");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_get_debug_register")]
    public async Task<string> GetDebugRegister(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Getting debug register");
        try
        {
            var value = await _client.GetDebugRegisterAsync(cancellationToken);
            return SerializeResponse(new { value });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to get debug register");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to get debug register");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_set_debug_register")]
    public async Task<string> SetDebugRegister(string value, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Setting debug register to {value}");
        try
        {
            var result = await _client.SetDebugRegisterAsync(value, cancellationToken);
            return SerializeResponse(new { value = result });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to set debug register");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to set debug register");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Machine Control

    [McpServerTool(Name = "ultimate_reset_machine")]
    public async Task<string> ResetMachine(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Resetting machine");
        try
        {
            await _client.ResetMachineAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Machine reset" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset machine");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reset machine");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_reboot_device")]
    public async Task<string> RebootDevice(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Rebooting device");
        try
        {
            await _client.RebootMachineAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Device rebooting" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to reboot device");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to reboot device");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_pause_machine")]
    public async Task<string> PauseMachine(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Pausing machine");
        try
        {
            await _client.PauseMachineAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Machine paused" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to pause machine");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to pause machine");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_resume_machine")]
    public async Task<string> ResumeMachine(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Resuming machine");
        try
        {
            await _client.ResumeMachineAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Machine resumed" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to resume machine");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to resume machine");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_power_off")]
    public async Task<string> PowerOff(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Powering off device");
        try
        {
            await _client.PowerOffMachineAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Powering off" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to power off");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to power off");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Streaming

    [McpServerTool(Name = "ultimate_start_stream")]
    public async Task<string> StartStream(string stream, string ip, int? port = null, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Starting stream {stream} to {ip}:{port}");
        try
        {
            await _client.StartStreamAsync(stream, ip, port, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Stream {stream} started" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to start stream");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to start stream");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_stop_stream")]
    public async Task<string> StopStream(string stream, CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation($"Stopping stream {stream}");
        try
        {
            await _client.StopStreamAsync(stream, cancellationToken);
            return SerializeResponse(new { ok = true, message = $"Stream {stream} stopped" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to stop stream");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to stop stream");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "ultimate_menu_button")]
    public async Task<string> MenuButton(CancellationToken cancellationToken = default)
    {
        _serviceLogger.LogInformation("Pressing menu button");
        try
        {
            await _client.MenuButtonAsync(cancellationToken);
            return SerializeResponse(new { ok = true, message = "Menu button pressed" });
        }
        catch (UltimateClientException ex)
        {
            _serviceLogger.LogError(ex, "Failed to press menu button");
            return SerializeResponse(new { error = ex.Message, errors = ex.ApiErrors });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to press menu button");
            return SerializeResponse(new { error = ex.Message });
        }
    }

    // Program Generation

    [McpServerTool(Name = "ultimate_generate_basic_prg")]
    public Task<CallToolResult> GenerateBasicPrg(string basicSource)
    {
        _serviceLogger.LogInformation("Generating BASIC PRG from source code");
        try
        {
            var generator = new BasicPrgGenerator();
            byte[] prgBytes = generator.GeneratePrg(basicSource);
            string base64Prg = Convert.ToBase64String(prgBytes);
            
            _serviceLogger.LogInformation($"Generated PRG ({prgBytes.Length} bytes) from BASIC source ({basicSource.Length} chars)");
            
            return Task.FromResult(CreateToolResult(new 
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
            return Task.FromResult(CreateToolErrorResult(ex.Message, "SyntaxError"));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to generate BASIC PRG");
            return Task.FromResult(CreateToolErrorResult(ex.Message, "GenerationError"));
        }
    }

    [McpServerTool(Name = "ultimate_generate_assembly_prg")]
    public Task<CallToolResult> GenerateAssemblyPrg(string assemblySource, bool basicRunLoader = false)
    {
        _serviceLogger.LogInformation("Generating assembly PRG from 6510 source code");
        try
        {
            var generator = new AssemblyPrgGenerator();
            var result = generator.GeneratePrg(assemblySource, basicRunLoader);
            var prgBase64 = Convert.ToBase64String(result.PrgBytes);

            _serviceLogger.LogInformation(
                "Generated ASM PRG ({PrgBytes} bytes, origin ${Origin:X4}, basicRunLoader={BasicRunLoader})",
                result.PrgBytes.Length,
                result.Origin,
                basicRunLoader);

            return Task.FromResult(CreateToolResult(new
            {
                success = true,
                prgDataBase64 = prgBase64,
                sizeBytes = result.PrgBytes.Length,
                origin = $"0x{result.Origin:X4}",
                sysAddress = result.Origin,
                basicRunLoader,
                note = basicRunLoader
                    ? "PRG contains BASIC loader line 10 SYS<addr>; start with RUN."
                    : $"Start with SYS {result.Origin}."
            }));
        }
        catch (ArgumentException ex)
        {
            _serviceLogger.LogWarning(ex, "Assembly syntax/validation error");
            return Task.FromResult(CreateToolErrorResult(ex.Message, "AssemblySyntaxError"));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _serviceLogger.LogError(ex, "Failed to generate ASM PRG");
            return Task.FromResult(CreateToolErrorResult(ex.Message, "AssemblyGenerationError"));
        }
    }

    // MARK: Error Handling & Form Elicitation
    //
    // The UltimateService implements comprehensive error handling for form requests
    // and MCP task support. When a tool call fails:
    //
    // 1. UltimateClientException: Maps to API errors with detailed error messages
    //    - Includes API error details when available (e.g., device timeout)
    // 
    // 2. Validation Errors: Caught early with descriptive messages
    //    - File not found, invalid parameters, insufficient data
    //
    // 3. Network/IO Errors: Logged with full context
    //    - HTTP failures, file access issues
    //
    // 4. Form Elicitation: Tools can return structured error objects
    //    - Client can use these errors to prompt user for missing/invalid input
    //    - Supports iterative user interaction patterns
    //
    // 5. Task Support: Long-running operations (e.g., PlaySidBinary, LoadProgram)
    //    - Use async/await pattern throughout
    //    - Proper error propagation via SerializeResponse
    //    - Session-aware logging for debugging
    //
    // Example error response format (MCP SDK compatible):
    // { "error": "Device timeout", "errors": ["Connection failed"], "code": "TIMEOUT" }
    public void Dispose() => _client.Dispose();
}







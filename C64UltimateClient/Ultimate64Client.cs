using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace C64UltimateClient;

/// <summary>
/// Client exception for API errors and other operation failures.
/// </summary>
public class UltimateClientException : Exception
{
    public IReadOnlyList<string>? ApiErrors { get; set; }
    /// <summary>HTTP status reported by the device, when available.</summary>
    public int? StatusCode { get; set; }

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
    private readonly string? _networkPassword;

    public Ultimate64Client(string baseUrl, ILogger<Ultimate64Client>? logger = null, TimeSpan? timeout = null)
        : this(new UltimateClientOptions { BaseUrl = baseUrl }, new SocketsHttpHandler { AllowAutoRedirect = false }, logger, timeout) { }

    /// <summary>Creates a client with an owned transport. Supplied handlers must not follow redirects.</summary>
    public Ultimate64Client(UltimateClientOptions options, HttpMessageHandler handler, ILogger<Ultimate64Client>? logger = null, TimeSpan? timeout = null)
    {
        _logger = logger;
        _httpClient = new HttpClient(ConfigureTransport(handler), disposeHandler: true) { BaseAddress = new Uri(options.BaseUrl) };
        _networkPassword = options.NetworkPassword;
        _httpClient.Timeout = timeout ?? options.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _logger?.LogInformation($"[Ultimate64Client] Initialized with BaseUrl: {options.BaseUrl}");
    }

    #region About

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<VersionResponse> GetVersionAsync() => GetVersionAsync(CancellationToken.None);

    /// <summary>
    /// GET /v1/version - Get API version
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GetVersionAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Get, "/v1/version", null, cancellationToken);
            return await HandleResponse<VersionResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get version");
            throw;
        }
    }

    #endregion

    #region Machine Control

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task ResetMachineAsync() => ResetMachineAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:reset - Soft reset (configuration preserved)
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task ResetMachineAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("ResetMachineAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:reset", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset machine");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task RebootMachineAsync() => RebootMachineAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:reboot - Full reboot (reinitialize cartridge)
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task RebootMachineAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("RebootMachineAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:reboot", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reboot machine");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PauseMachineAsync() => PauseMachineAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:pause - Pause CPU by pulling DMA line low
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PauseMachineAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("PauseMachineAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:pause", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pause machine");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task ResumeMachineAsync() => ResumeMachineAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:resume - Resume CPU from paused state
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task ResumeMachineAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("ResumeMachineAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:resume", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to resume machine");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PowerOffMachineAsync() => PowerOffMachineAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:poweroff - Power off (U64 only, may not return response)
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PowerOffMachineAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("PowerOffMachineAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:poweroff", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to power off machine");
            throw;
        }
    }

    #endregion

    #region Memory Operations

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<byte[]> ReadMemoryAsync(string address, int length = 256) => ReadMemoryAsync(address, length, CancellationToken.None);

    /// <summary>
    /// GET /v1/machine:readmem - Read from C64 memory via DMA
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "D000")</param>
    /// <param name="length">Number of bytes to read (default 256, max 65536)</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<byte[]> ReadMemoryAsync(string address, int length, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"ReadMemoryAsync address={address} length={length}");
        try
        {
            var url = $"/v1/machine:readmem?address={address}&length={length}";
            using var response = await SendAsync(HttpMethod.Get, url, null, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                throw new UltimateClientException($"Failed to read memory: HTTP {response.StatusCode}");

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read memory");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task WriteMemoryAsync(string address, string data) => WriteMemoryAsync(address, data, CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:writemem - Write to C64 memory via DMA (query string method, max 128 bytes)
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "D020")</param>
    /// <param name="data">Bytes to write in hex format (e.g., "0504")</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task WriteMemoryAsync(string address, string data, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"WriteMemoryAsync address={address} data={data}");
        try
        {
            var url = $"/v1/machine:writemem?address={address}&data={data}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write memory");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task WriteMemoryBinaryAsync(string address, byte[] data) => WriteMemoryBinaryAsync(address, data, CancellationToken.None);

    /// <summary>
    /// POST /v1/machine:writemem - Write to C64 memory via DMA (binary method, supports >128 bytes)
    /// </summary>
    /// <param name="address">Memory address in hex format (e.g., "0800")</param>
    /// <param name="data">Binary data to write</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task WriteMemoryBinaryAsync(string address, byte[] data, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"WriteMemoryBinaryAsync address={address} size={data.Length}");
        try
        {
            var url = $"/v1/machine:writemem?address={address}";
            var content = new ByteArrayContent(data)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            using var response = await SendAsync(HttpMethod.Post, url, content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write memory binary");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<string> GetDebugRegisterAsync() => GetDebugRegisterAsync(CancellationToken.None);

    /// <summary>
    /// GET /v1/machine:debugreg - Read debug register $D7FF (U64 only)
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<string> GetDebugRegisterAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GetDebugRegisterAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Get, "/v1/machine:debugreg", null, cancellationToken);
            var result = await HandleResponse<JsonElement>(response, cancellationToken);
            return result.GetProperty("value").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get debug register");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<string> SetDebugRegisterAsync(string value) => SetDebugRegisterAsync(value, CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:debugreg - Write debug register $D7FF (U64 only)
    /// </summary>
    /// <param name="value">Value in hex format (e.g., "FF")</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<string> SetDebugRegisterAsync(string value, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"SetDebugRegisterAsync value={value}");
        try
        {
            var url = $"/v1/machine:debugreg?value={value}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            var result = await HandleResponse<JsonElement>(response, cancellationToken);
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

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PlaySidAsync(string file, int? songNumber = null) => PlaySidAsync(file, songNumber, CancellationToken.None);

    /// <summary>
    /// PUT /v1/runners:sidplay - Play SID file from filesystem
    /// </summary>
    /// <param name="file">Value for file.</param>
    /// <param name="songNumber">Value for songNumber.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PlaySidAsync(string file, int? songNumber, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"PlaySidAsync file={file} song={songNumber}");
        try
        {
            var queryParams = new List<string> { $"file={Uri.EscapeDataString(file)}" };
            if (songNumber.HasValue)
                queryParams.Add($"songnr={songNumber.Value}");

            var url = $"/v1/runners:sidplay?{string.Join("&", queryParams)}";
            _logger?.LogDebug($"PlaySidAsync URL: {url}");
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play SID");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PlaySidBinaryAsync(byte[] sidData, int? songNumber = null) => PlaySidBinaryAsync(sidData, songNumber, CancellationToken.None);

    /// <summary>
    /// POST /v1/runners:sidplay - Play SID file from binary data
    /// </summary>
    /// <param name="sidData">Value for sidData.</param>
    /// <param name="songNumber">Value for songNumber.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PlaySidBinaryAsync(byte[] sidData, int? songNumber, CancellationToken cancellationToken)
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
            using var response = await SendAsync(HttpMethod.Post, url, content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play SID binary");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PlayModAsync(string file) => PlayModAsync(file, CancellationToken.None);

    /// <summary>
    /// PUT /v1/runners:modplay - Play MOD file from filesystem
    /// </summary>
    /// <param name="file">Value for file.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PlayModAsync(string file, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"PlayModAsync file={file}");
        try
        {
            var url = $"/v1/runners:modplay?file={Uri.EscapeDataString(file)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play MOD");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task PlayModBinaryAsync(byte[] modData) => PlayModBinaryAsync(modData, CancellationToken.None);

    /// <summary>
    /// POST /v1/runners:modplay - Play MOD file from binary data
    /// </summary>
    /// <param name="modData">Value for modData.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task PlayModBinaryAsync(byte[] modData, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"PlayModBinaryAsync size={modData.Length}");
        try
        {
            var content = new ByteArrayContent(modData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            using var response = await SendAsync(HttpMethod.Post, "/v1/runners:modplay", content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to play MOD binary");
            throw;
        }
    }

    #endregion

    #region Program & Cartridge Execution

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task LoadProgramAsync(string file) => LoadProgramAsync(file, CancellationToken.None);

    /// <summary>
    /// PUT /v1/runners:load_prg - Load PRG file from filesystem (does not run)
    /// </summary>
    /// <param name="file">Value for file.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task LoadProgramAsync(string file, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"LoadProgramAsync file={file}");
        try
        {
            var url = $"/v1/runners:load_prg?file={Uri.EscapeDataString(file)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load program");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task RunProgramAsync(string file) => RunProgramAsync(file, CancellationToken.None);

    /// <summary>
    /// PUT /v1/runners:run_prg - Run PRG file from filesystem
    /// </summary>
    /// <param name="file">Value for file.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task RunProgramAsync(string file, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"RunProgramAsync file={file}");
        try
        {
            var url = $"/v1/runners:run_prg?file={Uri.EscapeDataString(file)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run program");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task RunProgramBinaryAsync(byte[] prgData) => RunProgramBinaryAsync(prgData, CancellationToken.None);

    /// <summary>
    /// POST /v1/runners:run_prg - Run PRG file from binary data
    /// </summary>
    /// <param name="prgData">Value for prgData.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task RunProgramBinaryAsync(byte[] prgData, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"RunProgramBinaryAsync size={prgData.Length}");
        try
        {
            var content = new ByteArrayContent(prgData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            using var response = await SendAsync(HttpMethod.Post, "/v1/runners:run_prg", content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run program binary");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task RunCartridgeAsync(string file) => RunCartridgeAsync(file, CancellationToken.None);

    /// <summary>
    /// PUT /v1/runners:run_crt - Run cartridge from filesystem
    /// </summary>
    /// <param name="file">Value for file.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task RunCartridgeAsync(string file, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"RunCartridgeAsync file={file}");
        try
        {
            var url = $"/v1/runners:run_crt?file={Uri.EscapeDataString(file)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run cartridge");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task RunCartridgeBinaryAsync(byte[] crtData) => RunCartridgeBinaryAsync(crtData, CancellationToken.None);

    /// <summary>
    /// POST /v1/runners:run_crt - Run cartridge from binary data
    /// </summary>
    /// <param name="crtData">Value for crtData.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task RunCartridgeBinaryAsync(byte[] crtData, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"RunCartridgeBinaryAsync size={crtData.Length}");
        try
        {
            var content = new ByteArrayContent(crtData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            using var response = await SendAsync(HttpMethod.Post, "/v1/runners:run_crt", content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run cartridge binary");
            throw;
        }
    }

    #endregion

    #region Configuration Management

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<ConfigCategoriesResponse> GetConfigCategoriesAsync() => GetConfigCategoriesAsync(CancellationToken.None);

    /// <summary>
    /// GET /v1/configs - Get all configuration categories
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<ConfigCategoriesResponse> GetConfigCategoriesAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GetConfigCategoriesAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Get, "/v1/configs", null, cancellationToken);
            return await HandleResponse<ConfigCategoriesResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config categories");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<JsonElement> GetConfigCategoryAsync(string category) => GetConfigCategoryAsync(category, CancellationToken.None);

    /// <summary>
    /// GET /v1/configs/{category} - Get configuration items in a category (supports wildcards)
    /// </summary>
    /// <param name="category">Value for category.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<JsonElement> GetConfigCategoryAsync(string category, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"GetConfigCategoryAsync category={category}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}";
            using var response = await SendAsync(HttpMethod.Get, url, null, cancellationToken);
            return await HandleResponse<JsonElement>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config category");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<JsonElement> GetConfigItemAsync(string category, string item) => GetConfigItemAsync(category, item, CancellationToken.None);

    /// <summary>
    /// GET /v1/configs/{category}/{item} - Get specific configuration item (supports wildcards)
    /// </summary>
    /// <param name="category">Value for category.</param>
    /// <param name="item">Value for item.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<JsonElement> GetConfigItemAsync(string category, string item, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"GetConfigItemAsync category={category} item={item}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(item)}";
            using var response = await SendAsync(HttpMethod.Get, url, null, cancellationToken);
            return await HandleResponse<JsonElement>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get config item");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task SetConfigItemAsync(string category, string item, string value) => SetConfigItemAsync(category, item, value, CancellationToken.None);

    /// <summary>
    /// PUT /v1/configs/{category}/{item}?value={value} - Set configuration item
    /// </summary>
    /// <param name="category">Value for category.</param>
    /// <param name="item">Value for item.</param>
    /// <param name="value">Value for value.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task SetConfigItemAsync(string category, string item, string value, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"SetConfigItemAsync category={category} item={item} value={value}");
        try
        {
            var url = $"/v1/configs/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(item)}?value={Uri.EscapeDataString(value)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set config item");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task BulkConfigUpdateAsync(JsonElement config) => BulkConfigUpdateAsync(config, CancellationToken.None);

    /// <summary>
    /// POST /v1/configs - Bulk update multiple configuration items
    /// </summary>
    /// <param name="config">Value for config.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task BulkConfigUpdateAsync(JsonElement config, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("BulkConfigUpdateAsync");
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(config, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");
            
            using var response = await SendAsync(HttpMethod.Post, "/v1/configs", content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to bulk update config");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task SaveConfigAsync() => SaveConfigAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/configs:save_to_flash - Save current configuration to non-volatile memory
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task SaveConfigAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("SaveConfigAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/configs:save_to_flash", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save config");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task LoadConfigAsync() => LoadConfigAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/configs:load_from_flash - Load configuration from non-volatile memory
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task LoadConfigAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("LoadConfigAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/configs:load_from_flash", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load config");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task ResetConfigToDefaultAsync() => ResetConfigToDefaultAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/configs:reset_to_default - Reset configuration to factory defaults (does not clear saved values)
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task ResetConfigToDefaultAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("ResetConfigToDefaultAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/configs:reset_to_default", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset config to default");
            throw;
        }
    }

    #endregion

    #region Drive Management

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<DrivesResponse> GetDrivesInfoAsync() => GetDrivesInfoAsync(CancellationToken.None);

    /// <summary>
    /// GET /v1/drives - Get information about all drives
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<DrivesResponse> GetDrivesInfoAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GetDrivesInfoAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Get, "/v1/drives", null, cancellationToken);
            return await HandleResponse<DrivesResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get drives info");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task MountDiskAsync(string drive, string image, string? type = null, string? mode = null) => MountDiskAsync(drive, image, type, mode, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:mount - Mount disk image from filesystem
    /// </summary>
    /// <param name="drive">Drive identifier: "a", "b", "softiec", etc.</param>
    /// <param name="image">File path to disk image</param>
    /// <param name="type">Image type: "d64", "g64", "d71", "g71", "d81" (optional, auto-detected)</param>
    /// <param name="mode">"readwrite", "readonly", or "unlinked" (optional)</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task MountDiskAsync(string drive, string image, string? type, string? mode, CancellationToken cancellationToken)
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
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mount disk");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task MountDiskBinaryAsync(string drive, byte[] diskData, string? type = null, string? mode = null) => MountDiskBinaryAsync(drive, diskData, type, mode, CancellationToken.None);

    /// <summary>
    /// POST /v1/drives/{drive}:mount - Mount disk image from binary data
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="diskData">Value for diskData.</param>
    /// <param name="type">Value for type.</param>
    /// <param name="mode">Value for mode.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task MountDiskBinaryAsync(string drive, byte[] diskData, string? type, string? mode, CancellationToken cancellationToken)
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
            
            using var response = await SendAsync(HttpMethod.Post, url, content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mount disk binary");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task UnmountDiskAsync(string drive) => UnmountDiskAsync(drive, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:unmount or :remove - Unmount disk image
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task UnmountDiskAsync(string drive, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"UnmountDiskAsync drive={drive}");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, $"/v1/drives/{drive}:remove", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unmount disk");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task ResetDriveAsync(string drive) => ResetDriveAsync(drive, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:reset - Reset drive
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task ResetDriveAsync(string drive, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"ResetDriveAsync drive={drive}");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, $"/v1/drives/{drive}:reset", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset drive");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task SetDriveModeAsync(string drive, string mode) => SetDriveModeAsync(drive, mode, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:set_mode - Change drive mode/type
    /// </summary>
    /// <param name="drive">Drive identifier</param>
    /// <param name="mode">"1541", "1571", or "1581"</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task SetDriveModeAsync(string drive, string mode, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"SetDriveModeAsync drive={drive} mode={mode}");
        try
        {
            var url = $"/v1/drives/{drive}:set_mode?mode={mode}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set drive mode");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task EnableDriveAsync(string drive) => EnableDriveAsync(drive, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:on - Enable drive
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task EnableDriveAsync(string drive, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"EnableDriveAsync drive={drive}");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, $"/v1/drives/{drive}:on", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enable drive");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task DisableDriveAsync(string drive) => DisableDriveAsync(drive, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:off - Disable drive
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task DisableDriveAsync(string drive, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"DisableDriveAsync drive={drive}");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, $"/v1/drives/{drive}:off", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to disable drive");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task LoadDriveRomAsync(string drive, string file) => LoadDriveRomAsync(drive, file, CancellationToken.None);

    /// <summary>
    /// PUT /v1/drives/{drive}:load_rom - Load custom drive ROM from filesystem
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="file">Value for file.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task LoadDriveRomAsync(string drive, string file, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"LoadDriveRomAsync drive={drive} file={file}");
        try
        {
            var url = $"/v1/drives/{drive}:load_rom?file={Uri.EscapeDataString(file)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load drive ROM");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task LoadDriveRomBinaryAsync(string drive, byte[] romData) => LoadDriveRomBinaryAsync(drive, romData, CancellationToken.None);

    /// <summary>
    /// POST /v1/drives/{drive}:load_rom - Load custom drive ROM from binary data
    /// </summary>
    /// <param name="drive">Value for drive.</param>
    /// <param name="romData">Value for romData.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task LoadDriveRomBinaryAsync(string drive, byte[] romData, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"LoadDriveRomBinaryAsync drive={drive} size={romData.Length}");
        try
        {
            var content = new ByteArrayContent(romData)
            {
                Headers = { { "Content-Type", "application/octet-stream" } }
            };
            
            using var response = await SendAsync(HttpMethod.Post, $"/v1/drives/{drive}:load_rom", content, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load drive ROM binary");
            throw;
        }
    }

    #endregion

    #region Data Streams (U64 only)

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task StartStreamAsync(string streamName, string ipAddress, int? port = null) => StartStreamAsync(streamName, ipAddress, port, CancellationToken.None);

    /// <summary>
    /// PUT /v1/streams/{stream}:start - Start video/audio/debug stream
    /// </summary>
    /// <param name="streamName">"video", "audio", or "debug"</param>
    /// <param name="ipAddress">IP address to send stream to</param>
    /// <param name="port">Port number (default: 11000 for video, 11001 for audio, 11002 for debug)</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task StartStreamAsync(string streamName, string ipAddress, int? port, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"StartStreamAsync stream={streamName} ip={ipAddress} port={port}");
        try
        {
            var target = port.HasValue ? $"{ipAddress}:{port}" : ipAddress;
            var url = $"/v1/streams/{streamName}:start?ip={Uri.EscapeDataString(target)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start stream");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task StopStreamAsync(string streamName) => StopStreamAsync(streamName, CancellationToken.None);

    /// <summary>
    /// PUT /v1/streams/{stream}:stop - Stop video/audio/debug stream
    /// </summary>
    /// <param name="streamName">Value for streamName.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task StopStreamAsync(string streamName, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"StopStreamAsync stream={streamName}");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, $"/v1/streams/{streamName}:stop", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop stream");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task MenuButtonAsync() => MenuButtonAsync(CancellationToken.None);

    /// <summary>
    /// PUT /v1/machine:menu_button - Press the menu button
    /// Enters or exits the Ultimate menu depending on current state
    /// </summary>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task MenuButtonAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("MenuButtonAsync");
        try
        {
            using var response = await SendAsync(HttpMethod.Put, "/v1/machine:menu_button", null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to press menu button");
            throw;
        }
    }

    #endregion

    #region File Operations

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task<JsonElement> GetFileInfoAsync(string path) => GetFileInfoAsync(path, CancellationToken.None);

    /// <summary>
    /// GET /v1/files/{path}:info - Get file information
    /// </summary>
    /// <param name="path">Value for path.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task<JsonElement> GetFileInfoAsync(string path, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"GetFileInfoAsync path={path}");
        try
        {
            var url = $"/v1/files/{Uri.EscapeDataString(path)}:info";
            using var response = await SendAsync(HttpMethod.Get, url, null, cancellationToken);
            return await HandleResponse<JsonElement>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get file info");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task CreateD64Async(string path, int tracks = 35, string? diskName = null) => CreateD64Async(path, tracks, diskName, CancellationToken.None);

    /// <summary>
    /// PUT /v1/files/{path}:create_d64 - Create D64 disk image
    /// </summary>
    /// <param name="path">Full file path for new D64</param>
    /// <param name="tracks">Number of tracks: 35 (default) or 40</param>
    /// <param name="diskName">Name for disk header (optional, defaults to filename)</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task CreateD64Async(string path, int tracks, string? diskName, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"CreateD64Async path={path} tracks={tracks} diskName={diskName}");
        try
        {
            var queryParams = new List<string> { $"tracks={tracks}" };
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_d64?{string.Join("&", queryParams)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D64");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task CreateD71Async(string path, string? diskName = null) => CreateD71Async(path, diskName, CancellationToken.None);

    /// <summary>
    /// PUT /v1/files/{path}:create_d71 - Create D71 disk image (70 tracks)
    /// </summary>
    /// <param name="path">Value for path.</param>
    /// <param name="diskName">Value for diskName.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task CreateD71Async(string path, string? diskName, CancellationToken cancellationToken)
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

            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D71");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task CreateD81Async(string path, string? diskName = null) => CreateD81Async(path, diskName, CancellationToken.None);

    /// <summary>
    /// PUT /v1/files/{path}:create_d81 - Create D81 disk image (160 tracks, 80 per side)
    /// </summary>
    /// <param name="path">Value for path.</param>
    /// <param name="diskName">Value for diskName.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task CreateD81Async(string path, string? diskName, CancellationToken cancellationToken)
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

            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create D81");
            throw;
        }
    }

    /// <summary>Compatibility overload without caller cancellation.</summary>
    public Task CreateDnpAsync(string path, int tracks, string? diskName = null) => CreateDnpAsync(path, tracks, diskName, CancellationToken.None);

    /// <summary>
    /// PUT /v1/files/{path}:create_dnp - Create DNP disk image
    /// </summary>
    /// <param name="path">Full file path for new DNP</param>
    /// <param name="tracks">Number of tracks (required, each track = 256 sectors, max 255)</param>
    /// <param name="diskName">Name for disk header (optional)</param>
    /// <param name="cancellationToken">Cancels the HTTP request and response read.</param>
    public async Task CreateDnpAsync(string path, int tracks, string? diskName, CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"CreateDnpAsync path={path} tracks={tracks} diskName={diskName}");
        try
        {
            var queryParams = new List<string> { $"tracks={tracks}" };
            if (!string.IsNullOrEmpty(diskName))
                queryParams.Add($"diskname={Uri.EscapeDataString(diskName)}");

            var url = $"/v1/files/{Uri.EscapeDataString(path)}:create_dnp?{string.Join("&", queryParams)}";
            using var response = await SendAsync(HttpMethod.Put, url, null, cancellationToken);
            await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create DNP");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private static HttpMessageHandler ConfigureTransport(HttpMessageHandler handler)
    {
        var current = handler;
        while (current is DelegatingHandler { InnerHandler: { } inner }) current = inner;
        if (current is SocketsHttpHandler sockets) sockets.AllowAutoRedirect = false;
        if (current is HttpClientHandler http) http.AllowAutoRedirect = false;
        return handler;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        if (!string.IsNullOrEmpty(_networkPassword)) request.Headers.Add("X-Password", _networkPassword);
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private string Redact(string text) => string.IsNullOrEmpty(_networkPassword) ? text : text.Replace(_networkPassword, "[redacted]", StringComparison.Ordinal);

    private UltimateClientException DeviceError(HttpResponseMessage response, string body)
    {
        List<string>? errors = null;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("errors", out var values) && values.ValueKind == JsonValueKind.Array)
                errors = values.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => Redact(v.GetString()!)).ToList();
        }
        catch (JsonException) { }
        return new UltimateClientException($"API error: HTTP {(int)response.StatusCode}", errors) { StatusCode = (int)response.StatusCode };
    }



    private async Task<T> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogError($"Device returned HTTP {response.StatusCode}");
            throw DeviceError(response, error);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // Try to parse JSON response
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new UltimateClientException("Invalid JSON response: an object was expected.");
            
            // Check for errors in response
            if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && 
                errorsElement.ValueKind == JsonValueKind.Array)
            {
                var errors = errorsElement.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String
                        ? Redact(e.GetString() ?? "Unknown error")
                        : throw new UltimateClientException("Invalid API errors array: strings were expected."))
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

    private async Task HandleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogError($"Device returned HTTP {response.StatusCode}");
            throw DeviceError(response, error);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse response for errors
        if (!string.IsNullOrEmpty(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new UltimateClientException("Invalid JSON response: an object was expected.");
                
                if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && 
                    errorsElement.ValueKind == JsonValueKind.Array)
                {
                    var errors = errorsElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String
                        ? Redact(e.GetString() ?? "Unknown error")
                        : throw new UltimateClientException("Invalid API errors array: strings were expected."))
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
                _logger?.LogInformation($"Response: {Redact(content)}");
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

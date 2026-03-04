using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.Reflection;
using System.Text;

namespace C64UltimateMcpServer.Resources;

/// <summary>
/// Exposes C64 BASIC, Assembly, and documentation resources embedded in the application.
/// Resources include BASIC specifications, examples, pitfalls, and more from the data directory.
/// </summary>
[McpServerResourceType]
public class C64ResourceProvider
{
    private readonly ILogger<C64ResourceProvider> _logger;
    private readonly string _basePath;

    public C64ResourceProvider(ILogger<C64ResourceProvider> logger)
    {
        _logger = logger;
        // Get the directory where the application is running
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
        _basePath = Path.Combine(assemblyDirectory, "Resources", "data");
        _logger.LogInformation($"C64ResourceProvider initialized with base path: {_basePath}");
    }

    // BASIC Resources

    [McpServerResource(
        UriTemplate = "c64://basic/spec",
        Name = "BASIC V2 Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetBasicSpec()
    {
        _logger.LogInformation("Fetching BASIC specification");
        var path = Path.Combine(_basePath, "basic", "basic-spec.md");
        return ReadResourceFile(path, "c64://basic/spec");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/pitfalls",
        Name = "BASIC Pitfalls and Gotchas",
        MimeType = "text/markdown")]
    public ResourceContents GetBasicPitfalls()
    {
        _logger.LogInformation("Fetching BASIC pitfalls");
        var path = Path.Combine(_basePath, "basic", "basic-pitfalls.md");
        return ReadResourceFile(path, "c64://basic/pitfalls");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/hello-world",
        Name = "BASIC Hello World Example",
        MimeType = "text/plain")]
    public ResourceContents GetBasicHelloWorld()
    {
        _logger.LogInformation("Fetching BASIC Hello World example");
        var path = Path.Combine(_basePath, "basic", "examples", "video", "hello-world.bas");
        return ReadResourceFile(path, "c64://basic/examples/hello-world");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/joystick",
        Name = "BASIC Joystick Input Example",
        MimeType = "text/plain")]
    public ResourceContents GetBasicJoystick()
    {
        _logger.LogInformation("Fetching BASIC joystick example");
        var path = Path.Combine(_basePath, "basic", "examples", "io", "joystick.bas");
        return ReadResourceFile(path, "c64://basic/examples/joystick");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/bounce",
        Name = "BASIC Bounce Animation",
        MimeType = "text/plain")]
    public ResourceContents GetBasicBounce()
    {
        _logger.LogInformation("Fetching BASIC bounce example");
        var path = Path.Combine(_basePath, "basic", "examples", "video", "bounce.bas");
        return ReadResourceFile(path, "c64://basic/examples/bounce");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/wave",
        Name = "BASIC Wave Animation",
        MimeType = "text/plain")]
    public ResourceContents GetBasicWave()
    {
        _logger.LogInformation("Fetching BASIC wave example");
        var path = Path.Combine(_basePath, "basic", "examples", "video", "wave.bas");
        return ReadResourceFile(path, "c64://basic/examples/wave");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/entchen-petscii",
        Name = "BASIC Entchen PETSCII Demo",
        MimeType = "text/plain")]
    public ResourceContents GetBasicEntchenPetscii()
    {
        _logger.LogInformation("Fetching BASIC entchen PETSCII example");
        var path = Path.Combine(_basePath, "basic", "examples", "video", "entchen-petscii.bas");
        return ReadResourceFile(path, "c64://basic/examples/entchen-petscii");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/games/snake",
        Name = "BASIC Snake Game Example",
        MimeType = "text/plain")]
    public ResourceContents GetBasicSnakeGame()
    {
        _logger.LogInformation("Fetching BASIC snake game example");
        var path = Path.Combine(_basePath, "basic", "examples", "games", "snake.bas");
        return ReadResourceFile(path, "c64://basic/examples/games/snake");
    }

    [McpServerResource(
        UriTemplate = "c64://basic/examples/games/tictactoe",
        Name = "BASIC Tic-Tac-Toe Game Example",
        MimeType = "text/plain")]
    public ResourceContents GetBasicTicTacToeGame()
    {
        _logger.LogInformation("Fetching BASIC tictactoe game example");
        var path = Path.Combine(_basePath, "basic", "examples", "games", "tictactoe64_03.bas");
        return ReadResourceFile(path, "c64://basic/examples/games/tictactoe");
    }

    // Assembly Resources

    [McpServerResource(
        UriTemplate = "c64://assembly/spec",
        Name = "6510 Assembly Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetAssemblySpec()
    {
        _logger.LogInformation("Fetching Assembly specification");
        var path = Path.Combine(_basePath, "assembly", "assembly-spec.md");
        return ReadResourceFile(path, "c64://assembly/spec");
    }

    // Memory Resources

    [McpServerResource(
        UriTemplate = "c64://memory/map",
        Name = "C64 Memory Map",
        MimeType = "text/markdown")]
    public ResourceContents GetMemoryMap()
    {
        _logger.LogInformation("Fetching memory map");
        var path = Path.Combine(_basePath, "memory", "memory-map.md");
        return ReadResourceFile(path, "c64://memory/map");
    }

    [McpServerResource(
        UriTemplate = "c64://memory/kernal",
        Name = "Kernal Memory Map",
        MimeType = "text/markdown")]
    public ResourceContents GetKernalMemoryMap()
    {
        _logger.LogInformation("Fetching Kernal memory map");
        var path = Path.Combine(_basePath, "memory", "kernal-memory-map.md");
        return ReadResourceFile(path, "c64://memory/kernal");
    }

    [McpServerResource(
        UriTemplate = "c64://memory/low",
        Name = "Low Memory Map",
        MimeType = "text/markdown")]
    public ResourceContents GetLowMemoryMap()
    {
        _logger.LogInformation("Fetching low memory map");
        var path = Path.Combine(_basePath, "memory", "low-memory-map.md");
        return ReadResourceFile(path, "c64://memory/low");
    }

    // VIC-II Resources

    [McpServerResource(
        UriTemplate = "c64://graphics/vic-spec",
        Name = "VIC-II Graphics Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetVicSpec()
    {
        _logger.LogInformation("Fetching VIC specification");
        var path = Path.Combine(_basePath, "graphics", "vic-spec.md");
        return ReadResourceFile(path, "c64://graphics/vic-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://graphics/charset",
        Name = "Character Set Reference",
        MimeType = "text/csv")]
    public ResourceContents GetCharacterSet()
    {
        _logger.LogInformation("Fetching character set");
        var path = Path.Combine(_basePath, "graphics", "character-set.csv");
        return ReadResourceFile(path, "c64://graphics/charset");
    }

    [McpServerResource(
        UriTemplate = "c64://graphics/petscii",
        Name = "PETSCII Style Guide",
        MimeType = "text/markdown")]
    public ResourceContents GetPetsciiGuide()
    {
        _logger.LogInformation("Fetching PETSCII style guide");
        var path = Path.Combine(_basePath, "graphics", "petscii-style-guide.md");
        return ReadResourceFile(path, "c64://graphics/petscii");
    }

    [McpServerResource(
        UriTemplate = "c64://graphics/sprite-charset-best-practices",
        Name = "Sprite and Charset Best Practices",
        MimeType = "text/markdown")]
    public ResourceContents GetSpriteCharsetBestPractices()
    {
        _logger.LogInformation("Fetching sprite/charset best practices");
        var path = Path.Combine(_basePath, "graphics", "sprite-charset-best-practices.md");
        return ReadResourceFile(path, "c64://graphics/sprite-charset-best-practices");
    }

    // SID Audio Resources

    [McpServerResource(
        UriTemplate = "c64://sound/sid-spec",
        Name = "SID Chip Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetSidSpec()
    {
        _logger.LogInformation("Fetching SID specification");
        var path = Path.Combine(_basePath, "sound", "sid-spec.md");
        return ReadResourceFile(path, "c64://sound/sid-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://sound/sid-programming",
        Name = "SID Programming Best Practices",
        MimeType = "text/markdown")]
    public ResourceContents GetSidProgramming()
    {
        _logger.LogInformation("Fetching SID programming guide");
        var path = Path.Combine(_basePath, "sound", "sid-programming-best-practices.md");
        return ReadResourceFile(path, "c64://sound/sid-programming");
    }

    [McpServerResource(
        UriTemplate = "c64://sound/sid-file-structure",
        Name = "SID File Structure",
        MimeType = "text/markdown")]
    public ResourceContents GetSidFileStructure()
    {
        _logger.LogInformation("Fetching SID file structure");
        var path = Path.Combine(_basePath, "sound", "sid-file-structure.md");
        return ReadResourceFile(path, "c64://sound/sid-file-structure");
    }

    [McpServerResource(
        UriTemplate = "c64://sound/sidwave",
        Name = "SIDWAVE Format Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetSidwaveSpec()
    {
        _logger.LogInformation("Fetching SIDWAVE specification");
        var path = Path.Combine(_basePath, "sound", "sidwave.md");
        return ReadResourceFile(path, "c64://sound/sidwave");
    }

    // I/O Resources

    [McpServerResource(
        UriTemplate = "c64://io/cia-spec",
        Name = "CIA Chip Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetCiaSpec()
    {
        _logger.LogInformation("Fetching CIA specification");
        var path = Path.Combine(_basePath, "io", "cia-spec.md");
        return ReadResourceFile(path, "c64://io/cia-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://io/io-spec",
        Name = "I/O Port Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetIoSpec()
    {
        _logger.LogInformation("Fetching I/O specification");
        var path = Path.Combine(_basePath, "io", "io-spec.md");
        return ReadResourceFile(path, "c64://io/io-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://io/joystick",
        Name = "Joystick Control Reference",
        MimeType = "text/markdown")]
    public ResourceContents GetJoystickReference()
    {
        _logger.LogInformation("Fetching joystick reference");
        var path = Path.Combine(_basePath, "io", "joystick.md");
        return ReadResourceFile(path, "c64://io/joystick");
    }

    [McpServerResource(
        UriTemplate = "c64://io/keyboard-c64",
        Name = "C64 Keyboard Matrix Reference",
        MimeType = "text/plain")]
    public ResourceContents GetKeyboardC64Reference()
    {
        _logger.LogInformation("Fetching C64 keyboard matrix reference");
        var path = Path.Combine(_basePath, "io", "keyboard-c64.txt");
        return ReadResourceFile(path, "c64://io/keyboard-c64");
    }

    [McpServerResource(
        UriTemplate = "c64://io/control-codes-c64",
        Name = "C64 Control Codes Reference",
        MimeType = "text/plain")]
    public ResourceContents GetControlCodesC64Reference()
    {
        _logger.LogInformation("Fetching C64 control codes reference");
        var path = Path.Combine(_basePath, "io", "control-codes-c64.txt");
        return ReadResourceFile(path, "c64://io/control-codes-c64");
    }

    // Drive Resources

    [McpServerResource(
        UriTemplate = "c64://drive/spec",
        Name = "1541 Drive Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetDriveSpec()
    {
        _logger.LogInformation("Fetching drive specification");
        var path = Path.Combine(_basePath, "drive", "drive-spec.md");
        return ReadResourceFile(path, "c64://drive/spec");
    }

    // Printer Resources

    [McpServerResource(
        UriTemplate = "c64://printer/commodore-spec",
        Name = "Commodore Printer Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetCommodorePrinterSpec()
    {
        _logger.LogInformation("Fetching Commodore printer specification");
        var path = Path.Combine(_basePath, "printer", "printer-commodore.md");
        return ReadResourceFile(path, "c64://printer/commodore-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://printer/epson-spec",
        Name = "Epson Printer Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetEpsonPrinterSpec()
    {
        _logger.LogInformation("Fetching Epson printer specification");
        var path = Path.Combine(_basePath, "printer", "printer-epson.md");
        return ReadResourceFile(path, "c64://printer/epson-spec");
    }

    [McpServerResource(
        UriTemplate = "c64://printer/spec",
        Name = "Printer Unified Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetPrinterSpec()
    {
        _logger.LogInformation("Fetching printer unified specification");
        var path = Path.Combine(_basePath, "printer", "printer-spec.md");
        return ReadResourceFile(path, "c64://printer/spec");
    }

    [McpServerResource(
        UriTemplate = "c64://printer/prompts",
        Name = "Printer Prompt Routing Guide",
        MimeType = "text/markdown")]
    public ResourceContents GetPrinterPromptsGuide()
    {
        _logger.LogInformation("Fetching printer prompts routing guide");
        var path = Path.Combine(_basePath, "printer", "printer-prompts.md");
        return ReadResourceFile(path, "c64://printer/prompts");
    }

    [McpServerResource(
        UriTemplate = "c64://printer/commodore-bitmap",
        Name = "Commodore Bitmap Printing",
        MimeType = "text/markdown")]
    public ResourceContents GetCommodoreBitmapPrinterSpec()
    {
        _logger.LogInformation("Fetching Commodore bitmap printer guide");
        var path = Path.Combine(_basePath, "printer", "printer-commodore-bitmap.md");
        return ReadResourceFile(path, "c64://printer/commodore-bitmap");
    }

    [McpServerResource(
        UriTemplate = "c64://printer/epson-bitmap",
        Name = "Epson Bitmap Printing",
        MimeType = "text/markdown")]
    public ResourceContents GetEpsonBitmapPrinterSpec()
    {
        _logger.LogInformation("Fetching Epson bitmap printer guide");
        var path = Path.Combine(_basePath, "printer", "printer-epson-bitmap.md");
        return ReadResourceFile(path, "c64://printer/epson-bitmap");
    }

    // API Resources

    [McpServerResource(
        UriTemplate = "c64://api/basic-api",
        Name = "BASIC API Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetBasicApiSpec()
    {
        _logger.LogInformation("Fetching BASIC API specification");
        var path = Path.Combine(_basePath, "api", "basic-api-spec.md");
        return ReadResourceFile(path, "c64://api/basic-api");
    }

    [McpServerResource(
        UriTemplate = "c64://api/kernal-api",
        Name = "Kernal API Specification",
        MimeType = "text/markdown")]
    public ResourceContents GetKernalApiSpec()
    {
        _logger.LogInformation("Fetching Kernal API specification");
        var path = Path.Combine(_basePath, "api", "kernal-api-spec.md");
        return ReadResourceFile(path, "c64://api/kernal-api");
    }

    // c64ref-based Resources

    [McpServerResource(
        UriTemplate = "c64://memory/symbols",
        Name = "C64 Memory Symbols",
        MimeType = "text/plain")]
    public ResourceContents GetMemorySymbols()
    {
        _logger.LogInformation("Fetching C64 memory symbols");
        var path = Path.Combine(_basePath, "memory", "symbols.txt");
        return ReadResourceFile(path, "c64://memory/symbols");
    }

    [McpServerResource(
        UriTemplate = "c64://disasm/basic-rom",
        Name = "C64 BASIC ROM Disassembly",
        MimeType = "text/plain")]
    public ResourceContents GetBasicRomDisassembly()
    {
        _logger.LogInformation("Fetching BASIC ROM disassembly");
        var path = Path.Combine(_basePath, "disasm", "basic-rom.txt");
        return ReadResourceFile(path, "c64://disasm/basic-rom");
    }

    [McpServerResource(
        UriTemplate = "c64://disasm/kernal-rom",
        Name = "C64 KERNAL ROM Disassembly",
        MimeType = "text/plain")]
    public ResourceContents GetKernalRomDisassembly()
    {
        _logger.LogInformation("Fetching KERNAL ROM disassembly");
        var path = Path.Combine(_basePath, "disasm", "kernal-rom.txt");
        return ReadResourceFile(path, "c64://disasm/kernal-rom");
    }

    // Helper methods

    private ResourceContents ReadResourceFile(string filePath, string uri)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Resource file not found: {filePath}");
                return new TextResourceContents
                {
                    Uri = uri,
                    Text = $"Resource not found: {filePath}",
                    MimeType = "text/plain"
                };
            }

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var mimeType = GetMimeTypeFromPath(filePath);

            return mimeType switch
            {
                "text/plain" or "text/markdown" or "text/csv" => new TextResourceContents
                {
                    Uri = uri,
                    Text = content,
                    MimeType = mimeType
                },
                "application/json" => new TextResourceContents
                {
                    Uri = uri,
                    Text = content,
                    MimeType = mimeType
                },
                _ => new TextResourceContents
                {
                    Uri = uri,
                    Text = content,
                    MimeType = "text/plain"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading resource file: {filePath}");
            return new TextResourceContents
            {
                Uri = uri,
                Text = $"Error reading resource: {ex.Message}",
                MimeType = "text/plain"
            };
        }
    }

    private string GetMimeTypeFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".bas" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            _ => "text/plain"
        };
    }
}

using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace C64UltimateMcpServer.Resources;

/// <summary>
/// Exposes curated C64 resources while keeping the public c64:// URI contract stable.
/// </summary>
[McpServerResourceType]
public class C64ResourceProvider
{
    private readonly ILogger<C64ResourceProvider> _logger;
    private readonly string _basePath;

    public C64ResourceProvider(ILogger<C64ResourceProvider> logger)
    {
        _logger = logger;
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
        _basePath = Path.Combine(assemblyDirectory, "Resources", "data");
        _logger.LogInformation("C64ResourceProvider initialized with base path: {BasePath}", _basePath);
    }

    [McpServerResource(
        UriTemplate = "c64://resources/index",
        Name = "C64 Resource Index",
        MimeType = "text/markdown")]
    [Description("Browse the curated C64 resource index before choosing a topic-specific document.")]
    public ResourceContents GetResourceIndex()
    {
        _logger.LogInformation("Fetching resource index");
        return new TextResourceContents
        {
            Uri = "c64://resources/index",
            Text = C64ResourceCatalog.BuildIndexMarkdown(),
            MimeType = "text/markdown"
        };
    }

    [McpServerResource(
        UriTemplate = "c64://resources/{category}/{slug}",
        Name = "C64 Resource Catalog Entry",
        MimeType = "text/markdown")]
    [Description("Resolve a catalogued C64 resource by category and slug.")]
    public ResourceContents GetCatalogResource(
        [AllowedValues("basic", "assembly", "memory", "graphics", "sound", "io", "drive", "printer", "api", "disasm", "examples", "sources")]
        string category,
        string slug)
    {
        var normalizedCategory = category.Trim().ToLowerInvariant();
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        if (!C64ResourceCatalog.ByCategoryAndSlug.TryGetValue(
                C64ResourceCatalog.MakeKey(normalizedCategory, normalizedSlug),
                out var resource))
        {
            throw CreateUnknownResourceException($"c64://resources/{normalizedCategory}/{normalizedSlug}");
        }

        return ReadCatalogResource(resource);
    }

    [McpServerResource(UriTemplate = "c64://basic/spec", Name = "BASIC V2 Specification", MimeType = "text/markdown")]
    [Description("Reference for Commodore BASIC V2 syntax, memory layout, and runtime behavior.")]
    public ResourceContents GetBasicSpec() => GetCatalogResourceByUri("c64://basic/spec");

    [McpServerResource(UriTemplate = "c64://basic/pitfalls", Name = "BASIC Pitfalls and Gotchas", MimeType = "text/markdown")]
    [Description("Common mistakes, compatibility traps, and debugging tips for BASIC V2.")]
    public ResourceContents GetBasicPitfalls() => GetCatalogResourceByUri("c64://basic/pitfalls");

    [McpServerResource(UriTemplate = "c64://basic/examples/hello-world", Name = "BASIC Hello World Example", MimeType = "text/plain")]
    [Description("Minimal BASIC listing that prints a greeting.")]
    public ResourceContents GetBasicHelloWorld() => GetCatalogResourceByUri("c64://basic/examples/hello-world");

    [McpServerResource(UriTemplate = "c64://basic/examples/joystick", Name = "BASIC Joystick Input Example", MimeType = "text/plain")]
    [Description("BASIC example that reads joystick input.")]
    public ResourceContents GetBasicJoystick() => GetCatalogResourceByUri("c64://basic/examples/joystick");

    [McpServerResource(UriTemplate = "c64://basic/examples/bounce", Name = "BASIC Bounce Animation", MimeType = "text/plain")]
    [Description("Simple bouncing animation in BASIC V2.")]
    public ResourceContents GetBasicBounce() => GetCatalogResourceByUri("c64://basic/examples/bounce");

    [McpServerResource(UriTemplate = "c64://basic/examples/wave", Name = "BASIC Wave Animation", MimeType = "text/plain")]
    [Description("Wave animation example for screen effects.")]
    public ResourceContents GetBasicWave() => GetCatalogResourceByUri("c64://basic/examples/wave");

    [McpServerResource(UriTemplate = "c64://basic/examples/entchen-petscii", Name = "BASIC Entchen PETSCII Demo", MimeType = "text/plain")]
    [Description("PETSCII demo showing a character-based display.")]
    public ResourceContents GetBasicEntchenPetscii() => GetCatalogResourceByUri("c64://basic/examples/entchen-petscii");

    [McpServerResource(UriTemplate = "c64://basic/examples/games/snake", Name = "BASIC Snake Game Example", MimeType = "text/plain")]
    [Description("Small snake game example in BASIC V2.")]
    public ResourceContents GetBasicSnakeGame() => GetCatalogResourceByUri("c64://basic/examples/games/snake");

    [McpServerResource(UriTemplate = "c64://basic/examples/games/tictactoe", Name = "BASIC Tic-Tac-Toe Game Example", MimeType = "text/plain")]
    [Description("Two-player tic-tac-toe example in BASIC V2.")]
    public ResourceContents GetBasicTicTacToeGame() => GetCatalogResourceByUri("c64://basic/examples/games/tictactoe");

    [McpServerResource(UriTemplate = "c64://assembly/spec", Name = "6510 Assembly Specification", MimeType = "text/markdown")]
    [Description("Overview of supported 6510 assembly syntax, addressing modes, and conventions.")]
    public ResourceContents GetAssemblySpec() => GetCatalogResourceByUri("c64://assembly/spec");

    [McpServerResource(UriTemplate = "c64://assembly/tooling-notes", Name = "6510 Assembly Tooling Notes", MimeType = "text/markdown")]
    [Description("Notes about assembly tooling and the source layout supported by ultimate_generate_assembly_prg.")]
    public ResourceContents GetAssemblyToolingNotes() => GetCatalogResourceByUri("c64://assembly/tooling-notes");

    [McpServerResource(UriTemplate = "c64://assembly/ml-kernal-quickstart", Name = "Machine Language KERNAL Quickstart", MimeType = "text/markdown")]
    [Description("Practical 6510 assembly quickstart for SYS loaders, screen output, keyboard polling, and RUN/STOP handling.")]
    public ResourceContents GetAssemblyMlKernalQuickstart() => GetCatalogResourceByUri("c64://assembly/ml-kernal-quickstart");

    [McpServerResource(UriTemplate = "c64://assembly/examples/hello-sys", Name = "6510 Hello SYS Example", MimeType = "text/plain")]
    [Description("Simple assembly example that boots with SYS and uses the built-in assembly PRG generator syntax.")]
    public ResourceContents GetAssemblyExampleMcpHelloSys() => GetCatalogResourceByUri("c64://assembly/examples/hello-sys");

    [McpServerResource(UriTemplate = "c64://assembly/examples/text-scroll", Name = "6510 Text Scroll Example", MimeType = "text/plain")]
    [Description("Text scrolling example using the built-in assembly PRG generator syntax.")]
    public ResourceContents GetAssemblyExampleMcpTextScroll() => GetCatalogResourceByUri("c64://assembly/examples/text-scroll");

    [McpServerResource(UriTemplate = "c64://memory/map", Name = "C64 Memory Map", MimeType = "text/markdown")]
    [Description("General memory layout for the C64.")]
    public ResourceContents GetMemoryMap() => GetCatalogResourceByUri("c64://memory/map");

    [McpServerResource(UriTemplate = "c64://memory/kernal", Name = "Kernal Memory Map", MimeType = "text/markdown")]
    [Description("KERNAL memory layout and address overview.")]
    public ResourceContents GetKernalMemoryMap() => GetCatalogResourceByUri("c64://memory/kernal");

    [McpServerResource(UriTemplate = "c64://memory/low", Name = "Low Memory Map", MimeType = "text/markdown")]
    [Description("Low-memory usage and reserved areas.")]
    public ResourceContents GetLowMemoryMap() => GetCatalogResourceByUri("c64://memory/low");

    [McpServerResource(UriTemplate = "c64://memory/mapping-notes", Name = "C64 Practical Memory Mapping Notes", MimeType = "text/markdown")]
    [Description("Practical notes for using C64 memory locations safely from BASIC and 6510 assembly.")]
    public ResourceContents GetMemoryMappingNotes() => GetCatalogResourceByUri("c64://memory/mapping-notes");

    [McpServerResource(UriTemplate = "c64://graphics/vic-spec", Name = "VIC-II Graphics Specification", MimeType = "text/markdown")]
    [Description("VIC-II video chip reference covering modes, registers, and screen behavior.")]
    public ResourceContents GetVicSpec() => GetCatalogResourceByUri("c64://graphics/vic-spec");

    [McpServerResource(UriTemplate = "c64://graphics/charset", Name = "Character Set Reference", MimeType = "text/csv")]
    [Description("Character set lookup table for C64 screen codes and glyph mapping.")]
    public ResourceContents GetCharacterSet() => GetCatalogResourceByUri("c64://graphics/charset");

    [McpServerResource(UriTemplate = "c64://graphics/petscii", Name = "PETSCII Style Guide", MimeType = "text/markdown")]
    [Description("Guidelines for PETSCII composition, layout, and stylistic constraints.")]
    public ResourceContents GetPetsciiGuide() => GetCatalogResourceByUri("c64://graphics/petscii");

    [McpServerResource(UriTemplate = "c64://graphics/sprite-charset-best-practices", Name = "Sprite and Charset Best Practices", MimeType = "text/markdown")]
    [Description("Practical advice for mixing sprites, charsets, and screen memory efficiently.")]
    public ResourceContents GetSpriteCharsetBestPractices() => GetCatalogResourceByUri("c64://graphics/sprite-charset-best-practices");

    [McpServerResource(UriTemplate = "c64://sound/sid-spec", Name = "SID Chip Specification", MimeType = "text/markdown")]
    [Description("Core SID chip reference with voices, registers, and synthesis capabilities.")]
    public ResourceContents GetSidSpec() => GetCatalogResourceByUri("c64://sound/sid-spec");

    [McpServerResource(UriTemplate = "c64://sound/sid-programming", Name = "SID Programming Best Practices", MimeType = "text/markdown")]
    [Description("Practical guidance for composing and programming stable SID playback routines.")]
    public ResourceContents GetSidProgramming() => GetCatalogResourceByUri("c64://sound/sid-programming");

    [McpServerResource(UriTemplate = "c64://sound/sid-file-structure", Name = "SID File Structure", MimeType = "text/markdown")]
    [Description("Explains SID file layout, metadata, and loading expectations.")]
    public ResourceContents GetSidFileStructure() => GetCatalogResourceByUri("c64://sound/sid-file-structure");

    [McpServerResource(UriTemplate = "c64://sound/sidwave", Name = "SIDWAVE Format Specification", MimeType = "text/markdown")]
    [Description("Reference for the SIDWAVE format and its expected data representation.")]
    public ResourceContents GetSidwaveSpec() => GetCatalogResourceByUri("c64://sound/sidwave");

    [McpServerResource(UriTemplate = "c64://io/cia-spec", Name = "CIA Chip Specification", MimeType = "text/markdown")]
    [Description("CIA register and timing reference.")]
    public ResourceContents GetCiaSpec() => GetCatalogResourceByUri("c64://io/cia-spec");

    [McpServerResource(UriTemplate = "c64://io/io-spec", Name = "I/O Port Specification", MimeType = "text/markdown")]
    [Description("I/O port behavior and usage reference.")]
    public ResourceContents GetIoSpec() => GetCatalogResourceByUri("c64://io/io-spec");

    [McpServerResource(UriTemplate = "c64://io/joystick", Name = "Joystick Control Reference", MimeType = "text/markdown")]
    [Description("Joystick mappings and interaction guidance.")]
    public ResourceContents GetJoystickReference() => GetCatalogResourceByUri("c64://io/joystick");

    [McpServerResource(UriTemplate = "c64://io/keyboard-c64", Name = "C64 Keyboard Matrix Reference", MimeType = "text/plain")]
    [Description("Keyboard matrix lookup table.")]
    public ResourceContents GetKeyboardC64Reference() => GetCatalogResourceByUri("c64://io/keyboard-c64");

    [McpServerResource(UriTemplate = "c64://io/control-codes-c64", Name = "C64 Control Codes Reference", MimeType = "text/plain")]
    [Description("Control code reference for screen and text output.")]
    public ResourceContents GetControlCodesC64Reference() => GetCatalogResourceByUri("c64://io/control-codes-c64");

    [McpServerResource(UriTemplate = "c64://drive/spec", Name = "1541 Drive Specification", MimeType = "text/markdown")]
    [Description("Reference for Commodore 1541 drive behavior, commands, and data layout.")]
    public ResourceContents GetDriveSpec() => GetCatalogResourceByUri("c64://drive/spec");

    [McpServerResource(UriTemplate = "c64://printer/commodore-spec", Name = "Commodore Printer Specification", MimeType = "text/markdown")]
    [Description("Commodore printer reference covering control model and device-specific behavior.")]
    public ResourceContents GetCommodorePrinterSpec() => GetCatalogResourceByUri("c64://printer/commodore-spec");

    [McpServerResource(UriTemplate = "c64://printer/epson-spec", Name = "Epson Printer Specification", MimeType = "text/markdown")]
    [Description("Epson-compatible printer reference for control sequences and output behavior.")]
    public ResourceContents GetEpsonPrinterSpec() => GetCatalogResourceByUri("c64://printer/epson-spec");

    [McpServerResource(UriTemplate = "c64://printer/spec", Name = "Printer Unified Specification", MimeType = "text/markdown")]
    [Description("Unified printer overview bridging Commodore and Epson-oriented workflows.")]
    public ResourceContents GetPrinterSpec() => GetCatalogResourceByUri("c64://printer/spec");

    [McpServerResource(UriTemplate = "c64://printer/prompts", Name = "Printer Prompt Routing Guide", MimeType = "text/markdown")]
    [Description("Routing guide for choosing the right printer prompt or printer-related resource.")]
    public ResourceContents GetPrinterPromptsGuide() => GetCatalogResourceByUri("c64://printer/prompts");

    [McpServerResource(UriTemplate = "c64://printer/commodore-bitmap", Name = "Commodore Bitmap Printing", MimeType = "text/markdown")]
    [Description("Bitmap-printing guidance for Commodore printer workflows.")]
    public ResourceContents GetCommodoreBitmapPrinterSpec() => GetCatalogResourceByUri("c64://printer/commodore-bitmap");

    [McpServerResource(UriTemplate = "c64://printer/epson-bitmap", Name = "Epson Bitmap Printing", MimeType = "text/markdown")]
    [Description("Bitmap-printing guidance for Epson-compatible printer workflows.")]
    public ResourceContents GetEpsonBitmapPrinterSpec() => GetCatalogResourceByUri("c64://printer/epson-bitmap");

    [McpServerResource(UriTemplate = "c64://api/basic-api", Name = "BASIC API Specification", MimeType = "text/markdown")]
    [Description("API-oriented reference for BASIC-related routines, contracts, and integration points.")]
    public ResourceContents GetBasicApiSpec() => GetCatalogResourceByUri("c64://api/basic-api");

    [McpServerResource(UriTemplate = "c64://api/kernal-api", Name = "Kernal API Specification", MimeType = "text/markdown")]
    [Description("API-oriented reference for KERNAL routines, entry points, and calling expectations.")]
    public ResourceContents GetKernalApiSpec() => GetCatalogResourceByUri("c64://api/kernal-api");

    [McpServerResource(UriTemplate = "c64://examples/index", Name = "C64 Embedded Example Index", MimeType = "text/markdown")]
    [Description("MCP resource index for embedded C64 BASIC and assembly examples.")]
    public ResourceContents GetExampleSourceIndex() => GetCatalogResourceByUri("c64://examples/index");

    [McpServerResource(UriTemplate = "c64://examples/game-demo-patterns", Name = "Game And Demo Pattern Notes", MimeType = "text/markdown")]
    [Description("Embedded MCP notes for C64 game and demo patterns derived from MIT-licensed source examples.")]
    public ResourceContents GetExampleGameDemoPatterns() => GetCatalogResourceByUri("c64://examples/game-demo-patterns");

    [McpServerResource(UriTemplate = "c64://examples/assembly/dreadline-fastscroll-generator", Name = "Dreadline Fastscroll Generator Example", MimeType = "text/plain")]
    [Description("Dreadline-derived row fastscroll adapted to compile with the built-in assembly PRG generator.")]
    public ResourceContents GetExampleAssemblyDreadlineFastscrollGenerator() => GetCatalogResourceByUri("c64://examples/assembly/dreadline-fastscroll-generator");

    [McpServerResource(UriTemplate = "c64://examples/assembly/raster-bars-demo", Name = "Raster Bars Assembly Demo", MimeType = "text/plain")]
    [Description("Raster colour bars demo skeleton using the built-in assembly PRG generator syntax.")]
    public ResourceContents GetExampleAssemblyRasterBarsDemo() => GetCatalogResourceByUri("c64://examples/assembly/raster-bars-demo");

    [McpServerResource(UriTemplate = "c64://examples/assembly/sprite-demo", Name = "Single Sprite Assembly Demo", MimeType = "text/plain")]
    [Description("Single sprite setup and movement demo using the built-in assembly PRG generator syntax.")]
    public ResourceContents GetExampleAssemblySpriteDemo() => GetCatalogResourceByUri("c64://examples/assembly/sprite-demo");

    [McpServerResource(UriTemplate = "c64://examples/assembly/joystick-game-loop", Name = "Joystick Game Loop Assembly Skeleton", MimeType = "text/plain")]
    [Description("Joystick-driven game loop skeleton using the built-in assembly PRG generator syntax.")]
    public ResourceContents GetExampleAssemblyJoystickGameLoop() => GetCatalogResourceByUri("c64://examples/assembly/joystick-game-loop");

    [McpServerResource(UriTemplate = "c64://examples/assembly/hires-fire-line-demo", Name = "HIRES Fire Line Demo", MimeType = "text/plain")]
    [Description("Embedded upstream HIRES line/fire demo source; reference material that depends on the line draw library resource.")]
    public ResourceContents GetExampleAssemblyHiresFireLineDemo() => GetCatalogResourceByUri("c64://examples/assembly/hires-fire-line-demo");

    [McpServerResource(UriTemplate = "c64://examples/assembly/hello-world-kernal", Name = "KERNAL Hello World Assembly Example", MimeType = "text/plain")]
    [Description("Adaptation of an upstream C64 assembly hello world example for the built-in assembly PRG generator.")]
    public ResourceContents GetExampleAssemblyHelloWorldKernal() => GetCatalogResourceByUri("c64://examples/assembly/hello-world-kernal");

    [McpServerResource(UriTemplate = "c64://examples/assembly/hires-dot-burst-generator", Name = "HIRES Dot Burst Generator Example", MimeType = "text/plain")]
    [Description("HIRES fire/line demo idea adapted to compile with the built-in assembly PRG generator.")]
    public ResourceContents GetExampleAssemblyHiresDotBurstGenerator() => GetCatalogResourceByUri("c64://examples/assembly/hires-dot-burst-generator");

    [McpServerResource(UriTemplate = "c64://examples/assembly/hires-line-draw-library", Name = "HIRES Line Draw Library", MimeType = "text/plain")]
    [Description("Embedded upstream HIRES line drawing and dot stepping library reference.")]
    public ResourceContents GetExampleAssemblyHiresLineDrawLibrary() => GetCatalogResourceByUri("c64://examples/assembly/hires-line-draw-library");

    [McpServerResource(UriTemplate = "c64://examples/basic/token-control-codes", Name = "BASIC Token And Control Codes Example", MimeType = "text/plain")]
    [Description("Adaptation of an upstream C64 BASIC tokenization example for the built-in BASIC PRG generator.")]
    public ResourceContents GetExampleBasicTokenControlCodes() => GetCatalogResourceByUri("c64://examples/basic/token-control-codes");

    [McpServerResource(UriTemplate = "c64://sources/classic-c64-references", Name = "Classic C64 Reference Provenance", MimeType = "text/markdown")]
    [Description("Provenance notes for classic C64 books and manuals used to curate embedded MCP resources.")]
    public ResourceContents GetClassicC64ReferenceSources() => GetCatalogResourceByUri("c64://sources/classic-c64-references");

    [McpServerResource(UriTemplate = "c64://memory/symbols", Name = "C64 Memory Symbols", MimeType = "text/plain")]
    [Description("Common C64 memory symbols and labels.")]
    public ResourceContents GetMemorySymbols() => GetCatalogResourceByUri("c64://memory/symbols");

    [McpServerResource(UriTemplate = "c64://disasm/basic-rom", Name = "C64 BASIC ROM Disassembly", MimeType = "text/plain")]
    [Description("Disassembly listing for the C64 BASIC ROM for low-level inspection and lookup.")]
    public ResourceContents GetBasicRomDisassembly() => GetCatalogResourceByUri("c64://disasm/basic-rom");

    [McpServerResource(UriTemplate = "c64://disasm/kernal-rom", Name = "C64 KERNAL ROM Disassembly", MimeType = "text/plain")]
    [Description("Disassembly listing for the C64 KERNAL ROM for low-level inspection and lookup.")]
    public ResourceContents GetKernalRomDisassembly() => GetCatalogResourceByUri("c64://disasm/kernal-rom");

    private ResourceContents GetCatalogResourceByUri(string uri)
    {
        if (!C64ResourceCatalog.ByUri.TryGetValue(uri, out var resource))
        {
            throw CreateUnknownResourceException(uri);
        }

        _logger.LogInformation("Fetching catalog resource {Uri}", uri);
        return ReadCatalogResource(resource);
    }

    private ResourceContents ReadCatalogResource(C64ResourceDefinition resource)
    {
        var filePath = C64ResourceCatalog.BuildAbsolutePath(_basePath, resource);

        if (!File.Exists(filePath))
        {
            var message = $"Resource file not found for {resource.Uri}: {filePath}";
            _logger.LogError("{Message}", message);
            throw new McpProtocolException(message, McpErrorCode.InternalError);
        }

        try
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);

            return new TextResourceContents
            {
                Uri = resource.Uri,
                Text = content,
                MimeType = resource.MimeType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource file {FilePath} for {Uri}", filePath, resource.Uri);
            throw new McpProtocolException($"Failed to read resource {resource.Uri}: {ex.Message}", McpErrorCode.InternalError);
        }
    }

    private static McpProtocolException CreateUnknownResourceException(string uri)
    {
        return new McpProtocolException($"Unknown C64 resource: {uri}", McpErrorCode.InvalidParams);
    }
}

using System.IO;
using System.Linq;

namespace C64UltimateMcpServer.Resources;

public sealed record C64ResourceDefinition(
    string Uri,
    string Category,
    string Slug,
    string Name,
    string Description,
    string MimeType,
    string[] RelativePathSegments,
    string[] Tags)
{
    public string RelativePath => string.Join('/', RelativePathSegments);
}

public static class C64ResourceCatalog
{
    public static readonly IReadOnlyList<C64ResourceDefinition> All =
    [
        new("c64://basic/spec", "basic", "spec", "BASIC V2 Specification", "Reference for Commodore BASIC V2 syntax, commands, and runtime behavior.", "text/markdown", ["basic", "basic-spec.md"], ["basic", "reference"]),
        new("c64://basic/pitfalls", "basic", "pitfalls", "BASIC Pitfalls and Gotchas", "Common mistakes, compatibility traps, and debugging tips for BASIC V2.", "text/markdown", ["basic", "basic-pitfalls.md"], ["basic", "safety"]),
        new("c64://basic/examples/hello-world", "basic", "examples/hello-world", "BASIC Hello World Example", "Minimal BASIC listing that prints a greeting.", "text/plain", ["basic", "examples", "video", "hello-world.bas"], ["basic", "example"]),
        new("c64://basic/examples/joystick", "basic", "examples/joystick", "BASIC Joystick Input Example", "BASIC example that reads joystick input.", "text/plain", ["basic", "examples", "io", "joystick.bas"], ["basic", "example", "input"]),
        new("c64://basic/examples/bounce", "basic", "examples/bounce", "BASIC Bounce Animation", "Simple bouncing animation in BASIC V2.", "text/plain", ["basic", "examples", "video", "bounce.bas"], ["basic", "example", "animation"]),
        new("c64://basic/examples/wave", "basic", "examples/wave", "BASIC Wave Animation", "Wave animation example for screen effects.", "text/plain", ["basic", "examples", "video", "wave.bas"], ["basic", "example", "animation"]),
        new("c64://basic/examples/entchen-petscii", "basic", "examples/entchen-petscii", "BASIC Entchen PETSCII Demo", "PETSCII demo showing a character-based display.", "text/plain", ["basic", "examples", "video", "entchen-petscii.bas"], ["basic", "example", "petscii"]),
        new("c64://basic/examples/games/snake", "basic", "examples/games/snake", "BASIC Snake Game Example", "Small snake game example in BASIC V2.", "text/plain", ["basic", "examples", "games", "snake.bas"], ["basic", "example", "game"]),
        new("c64://basic/examples/games/tictactoe", "basic", "examples/games/tictactoe", "BASIC Tic-Tac-Toe Game Example", "Two-player tic-tac-toe example in BASIC V2.", "text/plain", ["basic", "examples", "games", "tictactoe64_03.bas"], ["basic", "example", "game"]),
        new("c64://assembly/spec", "assembly", "spec", "6510 Assembly Specification", "Overview of supported syntax and assembly conventions.", "text/markdown", ["assembly", "assembly-spec.md"], ["assembly", "reference"]),
        new("c64://assembly/tooling-notes", "assembly", "tooling-notes", "6510 Assembly Tooling Notes", "Notes about assembly tooling and the source layout supported by ultimate_generate_assembly_prg.", "text/markdown", ["assembly", "assembler-tooling-notes.md"], ["assembly", "tooling"]),
        new("c64://assembly/ml-kernal-quickstart", "assembly", "ml-kernal-quickstart", "Machine Language KERNAL Quickstart", "Practical 6510 assembly quickstart for SYS loaders, screen output, keyboard polling, and RUN/STOP handling.", "text/markdown", ["assembly", "ml-kernal-quickstart.md"], ["assembly", "kernal", "quickstart"]),
        new("c64://assembly/examples/hello-sys", "assembly", "examples/hello-sys", "6510 Hello SYS Example", "Simple assembly example that boots with SYS and uses the built-in assembly PRG generator syntax.", "text/plain", ["assembly", "examples", "hello-sys.asm"], ["assembly", "example"]),
        new("c64://assembly/examples/text-scroll", "assembly", "examples/text-scroll", "6510 Text Scroll Example", "Text scrolling example using the built-in assembly PRG generator syntax.", "text/plain", ["assembly", "examples", "text-scroll.asm"], ["assembly", "example"]),
        new("c64://memory/map", "memory", "map", "C64 Memory Map", "General memory layout for the C64.", "text/markdown", ["memory", "memory-map.md"], ["memory", "reference"]),
        new("c64://memory/kernal", "memory", "kernal", "Kernal Memory Map", "KERNAL memory layout and address overview.", "text/markdown", ["memory", "kernal-memory-map.md"], ["memory", "reference"]),
        new("c64://memory/low", "memory", "low", "Low Memory Map", "Low-memory usage and reserved areas.", "text/markdown", ["memory", "low-memory-map.md"], ["memory", "reference"]),
        new("c64://memory/mapping-notes", "memory", "mapping-notes", "C64 Practical Memory Mapping Notes", "Practical notes for using C64 memory locations safely from BASIC and 6510 assembly.", "text/markdown", ["memory", "mapping-notes.md"], ["memory", "mapping", "safety"]),
        new("c64://graphics/vic-spec", "graphics", "vic-spec", "VIC-II Graphics Specification", "VIC-II registers, timing, and graphics setup reference.", "text/markdown", ["graphics", "vic-spec.md"], ["graphics", "reference"]),
        new("c64://graphics/charset", "graphics", "charset", "Character Set Reference", "Character set and glyph reference in CSV format.", "text/csv", ["graphics", "character-set.csv"], ["graphics", "reference"]),
        new("c64://graphics/petscii", "graphics", "petscii", "PETSCII Style Guide", "PETSCII composition and layout guidance.", "text/markdown", ["graphics", "petscii-style-guide.md"], ["graphics", "reference"]),
        new("c64://graphics/sprite-charset-best-practices", "graphics", "sprite-charset-best-practices", "Sprite and Charset Best Practices", "Practical advice for sprite and charset combinations.", "text/markdown", ["graphics", "sprite-charset-best-practices.md"], ["graphics", "guidance"]),
        new("c64://sound/sid-spec", "sound", "sid-spec", "SID Chip Specification", "SID register and sound chip reference.", "text/markdown", ["sound", "sid-spec.md"], ["sound", "reference"]),
        new("c64://sound/sid-programming", "sound", "sid-programming", "SID Programming Best Practices", "Programming advice for SID music and sound effects.", "text/markdown", ["sound", "sid-programming-best-practices.md"], ["sound", "guidance"]),
        new("c64://sound/sid-file-structure", "sound", "sid-file-structure", "SID File Structure", "SID file organization and structure overview.", "text/markdown", ["sound", "sid-file-structure.md"], ["sound", "reference"]),
        new("c64://sound/sidwave", "sound", "sidwave", "SIDWAVE Format Specification", "Specification for the SIDWAVE format.", "text/markdown", ["sound", "sidwave.md"], ["sound", "reference"]),
        new("c64://io/cia-spec", "io", "cia-spec", "CIA Chip Specification", "CIA register and timing reference.", "text/markdown", ["io", "cia-spec.md"], ["io", "reference"]),
        new("c64://io/io-spec", "io", "io-spec", "I/O Port Specification", "I/O port behavior and usage reference.", "text/markdown", ["io", "io-spec.md"], ["io", "reference"]),
        new("c64://io/joystick", "io", "joystick", "Joystick Control Reference", "Joystick mappings and interaction guidance.", "text/markdown", ["io", "joystick.md"], ["io", "input"]),
        new("c64://io/keyboard-c64", "io", "keyboard-c64", "C64 Keyboard Matrix Reference", "Keyboard matrix lookup table.", "text/plain", ["io", "keyboard-c64.txt"], ["io", "input"]),
        new("c64://io/control-codes-c64", "io", "control-codes-c64", "C64 Control Codes Reference", "Control code reference for screen and text output.", "text/plain", ["io", "control-codes-c64.txt"], ["io", "reference"]),
        new("c64://drive/spec", "drive", "spec", "1541 Drive Specification", "1541 drive commands and behavior reference.", "text/markdown", ["drive", "drive-spec.md"], ["drive", "reference"]),
        new("c64://printer/commodore-spec", "printer", "commodore-spec", "Commodore Printer Specification", "Commodore printer command reference.", "text/markdown", ["printer", "printer-commodore.md"], ["printer", "reference"]),
        new("c64://printer/epson-spec", "printer", "epson-spec", "Epson Printer Specification", "Epson printer command reference.", "text/markdown", ["printer", "printer-epson.md"], ["printer", "reference"]),
        new("c64://printer/spec", "printer", "spec", "Printer Unified Specification", "Unified printer guidance across supported devices.", "text/markdown", ["printer", "printer-spec.md"], ["printer", "reference"]),
        new("c64://printer/prompts", "printer", "prompts", "Printer Prompt Routing Guide", "Prompt routing guide for printer-related tasks.", "text/markdown", ["printer", "printer-prompts.md"], ["printer", "guidance"]),
        new("c64://printer/commodore-bitmap", "printer", "commodore-bitmap", "Commodore Bitmap Printing", "Bitmap printing guidance for Commodore printers.", "text/markdown", ["printer", "printer-commodore-bitmap.md"], ["printer", "guidance"]),
        new("c64://printer/epson-bitmap", "printer", "epson-bitmap", "Epson Bitmap Printing", "Bitmap printing guidance for Epson printers.", "text/markdown", ["printer", "printer-epson-bitmap.md"], ["printer", "guidance"]),
        new("c64://api/basic-api", "api", "basic-api", "BASIC API Specification", "API reference for BASIC-related helpers.", "text/markdown", ["api", "basic-api-spec.md"], ["api", "reference"]),
        new("c64://api/kernal-api", "api", "kernal-api", "Kernal API Specification", "API reference for Kernal-related helpers.", "text/markdown", ["api", "kernal-api-spec.md"], ["api", "reference"]),
        new("c64://examples/index", "examples", "index", "C64 Example Source Index", "MCP resource index for local C64 BASIC, assembly, and cc65 example sources.", "text/markdown", ["examples", "index.md"], ["examples", "source", "reference"]),
        new("c64://examples/c64-ai-toolchain-catalog", "examples", "c64-ai-toolchain-catalog", "C64AIToolChain Example Catalog", "MCP resource catalog for MIT-licensed C64AIToolChain C and assembly examples.", "text/markdown", ["examples", "c64-ai-toolchain-catalog.md"], ["examples", "cc65", "assembly", "reference"]),
        new("c64://examples/assembly/raster-bars-demo", "examples", "assembly/raster-bars-demo", "Raster Bars Assembly Demo", "Raster colour bars demo skeleton using the built-in assembly PRG generator syntax.", "text/plain", ["examples", "assembly", "raster-bars-demo.asm"], ["examples", "assembly", "demo"]),
        new("c64://examples/assembly/sprite-demo", "examples", "assembly/sprite-demo", "Single Sprite Assembly Demo", "Single sprite setup and movement demo using the built-in assembly PRG generator syntax.", "text/plain", ["examples", "assembly", "sprite-demo.asm"], ["examples", "assembly", "sprite"]),
        new("c64://examples/assembly/joystick-game-loop", "examples", "assembly/joystick-game-loop", "Joystick Game Loop Assembly Skeleton", "Joystick-driven game loop skeleton using the built-in assembly PRG generator syntax.", "text/plain", ["examples", "assembly", "joystick-game-loop.asm"], ["examples", "assembly", "game"]),
        new("c64://examples/assembly/mcp-c64-hello-world", "examples", "assembly/mcp-c64-hello-world", "MCP-C64 Hello World Assembly Example", "Adaptation of the mcp-c64 assembly hello world example for the built-in assembly PRG generator.", "text/plain", ["examples", "assembly", "mcp-c64-hello-world.asm"], ["examples", "assembly", "source"]),
        new("c64://examples/basic/mcp-c64-token-test", "examples", "basic/mcp-c64-token-test", "MCP-C64 BASIC Token Test Example", "Adaptation of the mcp-c64 BASIC tokenization example for the built-in BASIC PRG generator.", "text/plain", ["examples", "basic", "mcp-c64-token-test.bas"], ["examples", "basic", "source"]),
        new("c64://sources/classic-c64-references", "sources", "classic-c64-references", "Classic C64 Reference Source Catalog", "Local source catalog for classic C64 books and manuals used to curate MCP resources.", "text/markdown", ["sources", "classic-c64-references.md"], ["sources", "reference", "okf"]),
        new("c64://memory/symbols", "memory", "symbols", "C64 Memory Symbols", "Common C64 memory symbols and labels.", "text/plain", ["memory", "symbols.txt"], ["memory", "reference"]),
        new("c64://disasm/basic-rom", "disasm", "basic-rom", "C64 BASIC ROM Disassembly", "Disassembly extract for the BASIC ROM.", "text/plain", ["disasm", "basic-rom.txt"], ["disasm", "reference"]),
        new("c64://disasm/kernal-rom", "disasm", "kernal-rom", "C64 KERNAL ROM Disassembly", "Disassembly extract for the KERNAL ROM.", "text/plain", ["disasm", "kernal-rom.txt"], ["disasm", "reference"])
    ];

    public static readonly IReadOnlyDictionary<string, C64ResourceDefinition> ByUri =
        All.ToDictionary(resource => resource.Uri, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<string, C64ResourceDefinition> ByCategoryAndSlug =
        All.ToDictionary(resource => MakeKey(resource.Category, resource.Slug), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Categories =>
        All.Select(resource => resource.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category).ToArray();

    public static string BuildAbsolutePath(string basePath, C64ResourceDefinition resource)
    {
        return Path.Combine(new[] { basePath }.Concat(resource.RelativePathSegments).ToArray());
    }

    public static string MakeKey(string category, string slug) => $"{category.Trim().ToLowerInvariant()}::{slug.Trim().ToLowerInvariant()}";

    public static string BuildIndexMarkdown()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("# C64 Resource Index");
        builder.AppendLine();
        builder.AppendLine("Start here before generating code, choosing tools, or giving hardware guidance.");
        builder.AppendLine();
        builder.AppendLine("## How to use this index");
        builder.AppendLine("- Read `c64://resources/index` first.");
        builder.AppendLine("- Pick the closest category for the task.");
        builder.AppendLine("- Open the topic resource and, when useful, a matching example.");
        builder.AppendLine("- Use `c64://resources/{category}/{slug}` when a client works better with catalog-style resource lookup.");
        builder.AppendLine();
        builder.AppendLine("## Fast routing");
        builder.AppendLine("- BASIC programs: `c64://basic/spec`, `c64://basic/pitfalls`, then BASIC examples.");
        builder.AppendLine("- Assembly work: `c64://assembly/spec`, `c64://assembly/tooling-notes`, `c64://assembly/ml-kernal-quickstart`, then assembly examples.");
        builder.AppendLine("- Example source discovery: `c64://examples/index`.");
        builder.AppendLine("- SID and audio: `c64://sound/sid-spec`, `c64://sound/sid-programming`, `c64://sound/sidwave`.");
        builder.AppendLine("- VIC-II and PETSCII: `c64://graphics/vic-spec`, `c64://graphics/petscii`, `c64://graphics/charset`.");
        builder.AppendLine("- Memory and debugging: `c64://memory/map`, `c64://memory/low`, `c64://memory/kernal`, `c64://memory/mapping-notes`, `c64://memory/symbols`.");
        builder.AppendLine("- Drives, printer, and I/O: `c64://drive/spec`, `c64://printer/spec`, `c64://io/io-spec`, `c64://io/cia-spec`.");
        builder.AppendLine("- Source provenance: `c64://sources/classic-c64-references`.");
        builder.AppendLine();

        foreach (var category in Categories)
        {
            builder.AppendLine($"## {char.ToUpperInvariant(category[0])}{category[1..]}");
            foreach (var resource in All.Where(resource => resource.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(resource => resource.Slug, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"- `{resource.Uri}` - {resource.Description}");
            }
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}

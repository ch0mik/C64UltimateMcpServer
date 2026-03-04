using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace C64UltimateMcpServer;

[McpServerPromptType]
public class C64UltimatePrompts
{
    private static PromptMessage Msg(Role role, string text) => new()
    {
        Role = role,
        Content = new TextContentBlock { Text = text }
    };

    private static string NormalizeEnumArg(
        string? value,
        string argName,
        params string[] allowed)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized) || !allowed.Contains(normalized))
        {
            throw new McpProtocolException(
                $"Invalid argument '{argName}'. Expected one of: {string.Join(", ", allowed)}.",
                McpErrorCode.InvalidParams);
        }
        return normalized;
    }

    private static IEnumerable<PromptMessage> BuildPrompt(
        params string[] assistantSegments)
    {
        return assistantSegments.Select(segment => Msg(Role.Assistant, segment));
    }

    [McpServerPrompt(
        Name = "basic-program",
        Title = "BASIC Program Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Plan, implement, and verify Commodore BASIC v2 programs safely.")]
    public IEnumerable<PromptMessage> BasicProgramWorkflow()
    {
        return BuildPrompt(
            """
            You operate the Commodore 64 Ultimate through MCP tools and documentation resources.
            Review relevant resources before generating code or issuing device commands.
            """,
            """
            Safety guardrails:
            - Ask before reset, reboot, power-off, or destructive memory/disk operations.
            - Explain side effects and provide a recovery path.
            """,
            """
            BASIC workflow:
            - Restate requirements and reference c64://basic/spec and c64://basic/pitfalls.
            - Produce numbered BASIC V2 listing.
            - Describe verification using screen output and expected READY. state.
            """);
    }

    [McpServerPrompt(
        Name = "assembly-program",
        Title = "Assembly Program Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Author 6502/6510 assembly routines with precise hardware guidance.")]
    public IEnumerable<PromptMessage> AssemblyProgramWorkflow(
        [Description("Optional focus area for the routine")]
        [AllowedValues("sid", "vic", "cia", "multi")]
        string? hardware = null)
    {
        var focus = string.IsNullOrWhiteSpace(hardware)
            ? "State which hardware blocks (SID, VIC-II, CIA) are used and why."
            : NormalizeEnumArg(hardware, "hardware", "sid", "vic", "cia", "multi") switch
            {
                "sid" => "Focus on SID registers and timing (c64://sound/sid-spec).",
                "vic" => "Focus on VIC-II setup and raster timing (c64://graphics/vic-spec).",
                "cia" => "Focus on CIA timers/ports and interrupt hand-off (c64://io/cia-spec).",
                _ => "Coordinate SID, VIC-II, and CIA interactions with contention notes."
            };

        return BuildPrompt(
            """
            Assembly workflow:
            - Provide memory layout, zero-page usage, and register side effects.
            - Explain deployment path (PRG/ASM upload or memory write sequence).
            """,
            focus,
            """
            IRQ discipline:
            - Wrap install with SEI/CLI when needed.
            - Acknowledge IRQ sources and document timing assumptions.
            """);
    }

    [McpServerPrompt(
        Name = "sid-music",
        Title = "SID Composition Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Compose SID music with iterative verification.")]
    public IEnumerable<PromptMessage> SidMusicWorkflow()
    {
        return BuildPrompt(
            """
            SID composition workflow:
            - Summarize target style, tempo, and structure.
            - Reference c64://sound/sid-spec, c64://sound/sid-programming, and c64://sound/sidwave.
            - Provide playback and iteration plan.
            """,
            """
            SID feedback loop:
            - Start with waveform/ADSR choices.
            - Explain what to adjust after listening (envelope, tuning, rhythm).
            """);
    }

    [McpServerPrompt(
        Name = "graphics-demo",
        Title = "Graphics Demo Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Create VIC-II graphics demos with safe setup and validation.")]
    public IEnumerable<PromptMessage> GraphicsDemoWorkflow(
        [Description("Target VIC-II technique")]
        [AllowedValues("text", "multicolour", "bitmap", "sprite")]
        string? mode = null)
    {
        var modeGuidance = string.IsNullOrWhiteSpace(mode)
            ? "Choose mode explicitly: text, multicolour, bitmap, or sprite."
            : NormalizeEnumArg(mode, "mode", "text", "multicolour", "bitmap", "sprite") switch
            {
                "text" => "Focus on PETSCII/text screen composition and charset.",
                "multicolour" => "Focus on multicolour bitmap and shared colour registers.",
                "bitmap" => "Focus on bitmap memory layout, screen RAM, and colour RAM.",
                _ => "Focus on sprite pointers, data layout, and animation timing."
            };

        return BuildPrompt(
            """
            Graphics workflow:
            - Summarize VIC-II register writes and memory banking (c64://graphics/vic-spec).
            - Explain setup, validation, and teardown.
            """,
            modeGuidance);
    }

    [McpServerPrompt(
        Name = "printer-job",
        Title = "Printer Job Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Send formatted output to Commodore or Epson printers.")]
    public IEnumerable<PromptMessage> PrinterJobWorkflow(
        [Description("Printer family")]
        [AllowedValues("commodore", "epson")]
        string? printerType = null)
    {
        var printerGuidance = string.IsNullOrWhiteSpace(printerType)
            ? "Clarify printer type: commodore or epson."
            : NormalizeEnumArg(printerType, "printerType", "commodore", "epson") switch
            {
                "commodore" => "Use Commodore control sequences (c64://printer/commodore-spec).",
                _ => "Use Epson ESC/P sequences (c64://printer/epson-spec)."
            };

        return BuildPrompt(
            """
            Printer workflow:
            - Confirm printer type and device number.
            - Include open/write/close sequence and page eject (CHR$(12)) where appropriate.
            - Reference c64://printer/spec and c64://printer/prompts.
            """,
            printerGuidance);
    }

    [McpServerPrompt(
        Name = "memory-debug",
        Title = "Memory Debug Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Inspect or patch memory ranges with reversible steps.")]
    public IEnumerable<PromptMessage> MemoryDebugWorkflow()
    {
        return BuildPrompt(
            """
            Memory debugging workflow:
            - Restate target address range and expected side effects.
            - Read before write, document diff, and provide rollback.
            - Reference c64://memory/map, c64://memory/low, and c64://memory/kernal.
            """);
    }

    [McpServerPrompt(
        Name = "drive-manager",
        Title = "Drive Manager Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Mount, create, and control drives with verification steps.")]
    public IEnumerable<PromptMessage> DriveManagerWorkflow()
    {
        return BuildPrompt(
            """
            Drive management workflow:
            - Baseline current drive state before changes.
            - Warn about IEC side effects on running workloads.
            - Verify each step after mount/unmount/mode/power operations.
            - Reference c64://drive/spec.
            """);
    }

    [McpServerPrompt(
        Name = "game-loop-basic",
        Title = "BASIC Game Loop Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Design a performant C64 BASIC game loop (input, update, render).")]
    public IEnumerable<PromptMessage> GameLoopBasicPrompt()
    {
        return BuildPrompt(
            """
            Create a C64 BASIC game-loop plan:
            - Input phase (GET/keyboard scan, debounce strategy).
            - Update phase (state transitions, collisions, score).
            - Render phase (screen/color RAM updates).
            """,
            """
            Constraints:
            - Keep loop deterministic and avoid expensive full-screen redraws.
            - Prefer incremental updates and precomputed constants.
            - Provide profiling hints (where delays/frame pacing happen).
            """,
            """
            Reference resources:
            - c64://basic/spec
            - c64://basic/pitfalls
            - c64://basic/examples/games/snake
            - c64://basic/examples/games/tictactoe
            """);
    }

    [McpServerPrompt(
        Name = "petscii-ui-layout",
        Title = "PETSCII UI Layout Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Generate PETSCII-driven UI/board layouts with clear positioning rules.")]
    public IEnumerable<PromptMessage> PetsciiUiLayoutPrompt()
    {
        return BuildPrompt(
            """
            Build a PETSCII UI layout for C64:
            - Define screen zones (title, playfield, HUD, status messages).
            - Use explicit cursor movement and control codes.
            - Keep text and gameplay layers readable.
            """,
            """
            Include:
            - Border/frame glyph strategy
            - Color strategy for accessibility
            - Minimal redraw plan for runtime updates
            """,
            """
            Reference resources:
            - c64://graphics/petscii
            - c64://graphics/charset
            - c64://io/control-codes-c64
            """);
    }

    [McpServerPrompt(
        Name = "keyboard-input-c64",
        Title = "C64 Keyboard Input Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Design robust keyboard input handling for C64 programs.")]
    public IEnumerable<PromptMessage> KeyboardInputC64Prompt(
        [Description("Input mode")]
        [AllowedValues("get-loop", "hotkeys", "menu")]
        string mode = "get-loop")
    {
        var normalizedMode = NormalizeEnumArg(mode, "mode", "get-loop", "hotkeys", "menu");
        return BuildPrompt(
            $"""
            Design keyboard handling for mode: {normalizedMode}
            - Map keys and behavior (press, hold, repeat).
            - Prevent accidental double triggers.
            - Document unsupported keys and fallbacks.
            """,
            """
            Include:
            - Scan-to-action table
            - Debounce/repeat policy
            - Error handling for invalid input
            """,
            """
            Reference resources:
            - c64://io/keyboard-c64
            - c64://io/control-codes-c64
            - c64://io/joystick
            """);
    }

    [McpServerPrompt(
        Name = "sprite-marker-workflow",
        Title = "Sprite Marker Workflow",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Create and drive sprite markers for board/game selection UX.")]
    public IEnumerable<PromptMessage> SpriteMarkerWorkflowPrompt()
    {
        return BuildPrompt(
            """
            Build a sprite-marker workflow:
            - Define sprite data source and pointer setup.
            - Specify coordinates for selectable targets.
            - Update color/position for focus and animation feedback.
            """,
            """
            Safety and compatibility:
            - Document sprite register writes.
            - Explain overlap with text/background and priority bits.
            - Include teardown/reset path.
            """,
            """
            Reference resources:
            - c64://graphics/vic-spec
            - c64://graphics/sprite-charset-best-practices
            - c64://assembly/spec
            """);
    }

    [McpServerPrompt(
        Name = "sid-sfx-basic",
        Title = "SID SFX for BASIC Games",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Design compact SID sound effects suitable for BASIC-driven games.")]
    public IEnumerable<PromptMessage> SidSfxBasicPrompt(
        [Description("SFX style")]
        [AllowedValues("arcade", "soft", "retro-noise")]
        string style = "arcade")
    {
        var normalizedStyle = NormalizeEnumArg(style, "style", "arcade", "soft", "retro-noise");
        return BuildPrompt(
            $"""
            Create SID SFX plan for BASIC games (style: {normalizedStyle}):
            - Define 3-5 effects (move, hit, score, game-over, pickup).
            - Keep patches short and reusable.
            - Explain trigger timing from game loop.
            """,
            """
            Include:
            - Register-level hints (waveform/ADSR/frequency)
            - Loudness and clipping precautions
            - Verification steps after playback
            """,
            """
            Reference resources:
            - c64://sound/sid-spec
            - c64://sound/sid-programming
            - c64://sound/sidwave
            """);
    }

    // Backward-compatible aliases

    [McpServerPrompt(
        Name = "c64_basic_program_prompt",
        Title = "C64 BASIC Program Prompt",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Creates a ready-to-use prompt for writing a BASIC V2 program for C64.")]
    public IEnumerable<PromptMessage> BasicProgramPrompt(
        [Description("What the program should do")]
        string goal,
        [Description("Complexity level of the generated program")]
        [AllowedValues("beginner", "intermediate", "advanced")]
        string level = "beginner")
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            throw new McpProtocolException("Missing required argument: goal", McpErrorCode.InvalidParams);
        }

        var normalizedLevel = (level ?? "beginner").Trim().ToLowerInvariant();
        if (normalizedLevel is not ("beginner" or "intermediate" or "advanced"))
        {
            throw new McpProtocolException("Invalid argument 'level'. Expected: beginner, intermediate, or advanced.", McpErrorCode.InvalidParams);
        }

        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text =
                    $"""
                    Write a Commodore 64 BASIC V2 program.
                    Goal: {goal}
                    Level: {normalizedLevel}

                    Constraints:
                    - Use line numbers.
                    - Keep compatibility with standard C64 BASIC V2.
                    - Return code first, then a short explanation.
                    """
                }
            }
        ];
    }

    [McpServerPrompt(
        Name = "c64_sid_music_prompt",
        Title = "C64 SID Music Prompt",
        IconSource = "https://upload.wikimedia.org/wikipedia/commons/2/23/Commodore_C%3D_logo.svg")]
    [Description("Creates a prompt for generating SID composition guidance.")]
    public IEnumerable<PromptMessage> SidMusicPrompt(
        [Description("Mood or style of the track")]
        [AllowedValues("chiptune", "ambient", "action", "retro-pop")]
        string style = "chiptune",
        [Description("Tempo in BPM")]
        int bpm = 120)
    {
        var normalizedStyle = (style ?? "chiptune").Trim().ToLowerInvariant();
        if (normalizedStyle is not ("chiptune" or "ambient" or "action" or "retro-pop"))
        {
            throw new McpProtocolException("Invalid argument 'style'. Expected: chiptune, ambient, action, or retro-pop.", McpErrorCode.InvalidParams);
        }

        if (bpm < 40 || bpm > 240)
        {
            throw new McpProtocolException("Invalid argument 'bpm'. Expected value between 40 and 240.", McpErrorCode.InvalidParams);
        }

        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text =
                    $"""
                    Prepare a SID composition plan for Commodore 64.
                    Style: {normalizedStyle}
                    Tempo: {bpm} BPM

                    Include:
                    - Instrument idea per channel
                    - Pattern structure
                    - Register-level hints where useful
                    """
                }
            }
        ];
    }
}

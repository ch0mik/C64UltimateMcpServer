---
type: Reference
title: C64AIToolChain Example Catalog
description: MCP resource catalog for MIT-licensed C64AIToolChain C and assembly examples.
resource: c64://examples/c64-ai-toolchain-catalog
tags:
- examples
- cc65
- assembly
- reference
---

# C64AIToolChain Example Catalog

`C64AIToolChain` is a local MIT-licensed C64 project collection. Use this catalog to find example source material for game loops, effects, sprites, scoring, collision logic, and visual-debug workflows.

Current MCP generators support BASIC and a compact assembly dialect. Treat C/cc65 examples as reference material unless a future cc65 workflow is added.

## Best First Assembly References

| Example | Local path | Why it matters |
| --- | --- | --- |
| Snake | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\snake\snake.s` | Small assembly game loop, joystick/demo control, screen memory use. |
| Snake 2 | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\snake2\snake.s` | Larger optimized assembly version with notes and listing output. |
| Tetris v1 | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\tetris_v1\tetris.s` | Block-game state, board layout, line clearing. |
| Tetris v2 | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\tetris_v2\tetris.s` | Smaller alternate Tetris implementation. |
| Pac-Man assembly | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\pacman\pacman.s` | Larger pure assembly game structure and movement logic. |
| Dreadline fast scroll | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\dreadline\fastscroll.s` | Focused assembly helper for optimized scrolling. |

## Best First C/cc65 References

| Example | Local path | Why it matters |
| --- | --- | --- |
| Starfield | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\starfield\starfield.c` | Compact visual effect, screen/color RAM, random movement. |
| Rasterbars | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\rasterbars\rasterbars.c` | Small raster/VIC colour effect reference. |
| Fire | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\fire\fire.c` | Compact procedural effect. |
| Plasma | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\plasma\plasma.c` | Compact classic demo effect. |
| Pong | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\pong\pong.c` | Simple game structure, input, collision, scoring. |
| Breakout | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\breakout\breakout.c` | Brick-grid collision and paddle/ball mechanics. |
| Arkanoid | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\arkanoid\arkanoid.c` | More advanced Breakout-like mechanics and levels. |
| Meteor | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\meteor\meteor.c` | Large original game with waves, power-ups, shields, and SID effects. |
| Dreadline | `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain\dreadline\dreadline.c` | Mixed C/assembly project with asset pipeline and scrolling. |

## Import Policy

- Prefer small sources for direct MCP resources.
- Keep large games as indexed references unless a specific task needs a curated excerpt.
- Do not present cc65 code as directly compatible with `ultimate_generate_assembly_prg`.
- Preserve MIT attribution if copying source into `Resources/data`.
- For generated assembly, translate concepts into the source syntax documented by `c64://assembly/tooling-notes` and supported by `ultimate_generate_assembly_prg`.

# Citations

[1] Local MIT-licensed repository: `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain`

[2] Related MCP resource: `c64://examples/index`

[3] Related MCP resource: `c64://assembly/tooling-notes`

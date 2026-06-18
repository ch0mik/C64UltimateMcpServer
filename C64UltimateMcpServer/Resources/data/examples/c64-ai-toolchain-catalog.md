---
type: Reference
title: C64AIToolChain-Derived Embedded Notes
description: Embedded MCP notes derived from MIT-licensed C64AIToolChain C and assembly examples.
resource: c64://examples/c64-ai-toolchain-catalog
tags:
- examples
- cc65
- assembly
- reference
---

# C64AIToolChain-Derived Embedded Notes

This resource captures portable, embedded notes derived from a review of the MIT-licensed C64AIToolChain examples. It intentionally avoids host-specific file paths so the MCP server works the same from a local build, a package, or a container.

Current MCP generators support BASIC and a compact assembly dialect. Treat C/cc65 examples as reference material unless a future cc65 workflow is added.

## Assembly Patterns To Reuse

| Pattern | Embedded MCP starting point | Why it matters |
| --- | --- | --- |
| Snake-style movement | `c64://examples/assembly/joystick-game-loop` | Small game loop, joystick control, screen memory state, and bounded movement. |
| Tetris-style board state | `c64://memory/mapping-notes` plus `c64://examples/assembly/joystick-game-loop` | Keep board arrays compact, separate input/update/draw phases, and document zero-page scratch use. |
| Pac-Man-style actors | `c64://examples/assembly/joystick-game-loop` | Model each actor as position/direction/state bytes, then update one actor per frame or per tick. |
| Fast scroll helper | `c64://assembly/examples/text-scroll` | Demonstrates softscroll plus hardscroll concepts in a generator-friendly source shape. |

## C/cc65 Patterns To Translate

| Pattern | Translation target | Why it matters |
| --- | --- | --- |
| Starfield | BASIC arrays or assembly screen RAM writes | Compact visual effect using screen/color RAM and pseudo-random movement. |
| Raster bars | `c64://examples/assembly/raster-bars-demo` | Small VIC colour effect reference for frame pacing and border/background updates. |
| Fire | BASIC or assembly table update loop | Procedural effect pattern: update a buffer, diffuse/decay values, map to colour RAM. |
| Plasma | BASIC precomputed tables or assembly byte tables | Classic demo effect based on lookup tables and repeated screen/color updates. |
| Pong | BASIC or assembly game loop | Simple input, collision, scoring, and frame update structure. |
| Breakout/Arkanoid | BASIC or assembly tile grid | Brick-grid collision, paddle/ball mechanics, levels, and score state. |
| Meteor-style waves | Assembly game loop plus SID notes | Larger game structure with waves, power-ups, shields, and sound effects. |
| Dreadline-style scrolling | `c64://assembly/examples/text-scroll` | Mixed scrolling concepts; translate to the supported assembly source format before compiling. |

## Import Policy

- Prefer small embedded sources for direct MCP resources.
- Keep large games as summarized reference patterns unless a specific task adds a curated excerpt under `Resources/data`.
- Do not present cc65 code as directly compatible with `ultimate_generate_assembly_prg`.
- Preserve MIT attribution if copying source into `Resources/data`.
- For generated assembly, translate concepts into the source syntax documented by `c64://assembly/tooling-notes` and supported by `ultimate_generate_assembly_prg`.

# Citations

[1] Source family: MIT-licensed C64AIToolChain examples, summarized here as embedded MCP knowledge.

[2] Related MCP resource: `c64://examples/index`

[3] Related MCP resource: `c64://assembly/tooling-notes`

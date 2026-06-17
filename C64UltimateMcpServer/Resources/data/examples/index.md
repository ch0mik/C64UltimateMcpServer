---
type: Reference
title: C64 Example Source Index
description: MCP resource index for local C64 BASIC, assembly, and cc65 example sources.
resource: c64://examples/index
tags:
- examples
- source
- reference
---

# C64 Example Source Index

This resource routes agents to local example sources that can inform generated C64 programs. Examples listed here are not all directly supported by the current MCP generators.

## Recommended First Imports

| Source | Local file | MCP fit |
| --- | --- | --- |
| MCP raster bars demo | `c64://examples/assembly/raster-bars-demo` | Good first demo/effect skeleton for colour timing and RUN/STOP-friendly loops. |
| MCP sprite demo | `c64://examples/assembly/sprite-demo` | Good first sprite setup example with pointer, copied sprite data, movement, and cleanup. |
| MCP joystick game loop | `c64://examples/assembly/joystick-game-loop` | Good first game-loop skeleton using joystick port 2, screen RAM, and frame pacing. |
| mcp-c64 assembly hello world | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\asm\hello\hello_world.asm` | Good fit for `ultimate_generate_assembly_prg` after syntax compatibility review. |
| mcp-c64 BASIC token test | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\basic\token\token_test.bas` | Good fit for `ultimate_generate_basic_prg` after token/control-code compatibility review. |
| mcp-c64 fire demo | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\asm\graphics\fire_demo.asm` | Useful assembly graphics example; depends on included library. |
| mcp-c64 line draw library | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\asm\graphics\line_draw_library.asm` | Useful assembly library material; import only with a compatible include strategy. |

## Agent Starting Points

| Goal | Start with |
| --- | --- |
| Small assembly program | `c64://examples/assembly/mcp-c64-hello-world` |
| Visual demo | `c64://examples/assembly/raster-bars-demo` |
| Sprite-based demo/game | `c64://examples/assembly/sprite-demo` |
| Joystick-controlled game | `c64://examples/assembly/joystick-game-loop` |
| BASIC token/control-code behavior | `c64://examples/basic/mcp-c64-token-test` |
| Larger game references | `c64://examples/c64-ai-toolchain-catalog` |

## C64AIToolChain Reference Set

`C64AIToolChain` is MIT licensed and contains many complete C64 examples. Treat these as reference material until this MCP server has a cc65/C workflow.

| Area | Candidate files | Notes |
| --- | --- | --- |
| Assembly games | `snake/snake.s`, `snake2/snake.s`, `tetris_v1/tetris.s`, `tetris_v2/tetris.s`, `pacman/pacman.s` | Useful for gameplay loops, zero-page layout, joystick handling, and AI/demo behavior. |
| Assembly helpers | `dreadline/fastscroll.s` | Useful for optimized scrolling patterns. |
| cc65 effects | `starfield/starfield.c`, `rasterbars/rasterbars.c`, `fire/fire.c`, `plasma/plasma.c`, `scroller/scroller.c` | Useful for C-level algorithms and visual effects, not directly compilable by current MCP tools. |
| cc65 games | `pong/pong.c`, `breakout/breakout.c`, `arkanoid/arkanoid.c`, `invaders/invaders.c`, `meteor/meteor.c`, `dreadline/dreadline.c` | Larger examples for game structure, assets, collisions, scoring, and sound. |

## Import Policy

- Only place examples in `Resources/data` when they are useful to MCP clients.
- Keep examples small enough to be read in one resource call where possible.
- Prefer working BASIC or single-file assembly examples before larger cc65 examples.
- Mark cc65 examples clearly as reference-only until a C compiler workflow exists.
- Preserve source attribution and license notes when copying from `C64AIToolChain`.

# Citations

[1] Local source repository: `C:\Retro\Commodore\c64-voice-controll\mcp-c64`

[2] Local MIT-licensed source repository: `C:\Retro\Commodore\c64-voice-controll\C64AIToolChain`

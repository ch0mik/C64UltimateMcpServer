---
type: Reference
title: C64 Embedded Example Index
description: MCP resource index for embedded C64 BASIC and assembly examples.
resource: c64://examples/index
tags:
- examples
- source
- reference
---

# C64 Embedded Example Index

This resource routes agents to examples embedded in the MCP server under `Resources/data`. It must remain portable across local runs, containers, and packaged deployments, so entries use `c64://` resource URIs instead of host-specific file paths.

## Recommended First Imports

| Source | Embedded resource | Fit |
| --- | --- | --- |
| MCP raster bars demo | `c64://examples/assembly/raster-bars-demo` | Good first demo/effect skeleton for colour timing and RUN/STOP-friendly loops. |
| MCP sprite demo | `c64://examples/assembly/sprite-demo` | Good first sprite setup example with pointer, copied sprite data, movement, and cleanup. |
| MCP joystick game loop | `c64://examples/assembly/joystick-game-loop` | Good first game-loop skeleton using joystick port 2, screen RAM, and frame pacing. |
| mcp-c64 assembly hello world adaptation | `c64://examples/assembly/mcp-c64-hello-world` | Good fit for `ultimate_generate_assembly_prg`; already adapted for the built-in assembly PRG generator. |
| mcp-c64 BASIC token test adaptation | `c64://examples/basic/mcp-c64-token-test` | Good fit for `ultimate_generate_basic_prg`; already embedded as a BASIC source resource. |

## Agent Starting Points

| Goal | Start with |
| --- | --- |
| Small assembly program | `c64://examples/assembly/mcp-c64-hello-world` |
| Visual demo | `c64://examples/assembly/raster-bars-demo` |
| Sprite-based demo/game | `c64://examples/assembly/sprite-demo` |
| Joystick-controlled game | `c64://examples/assembly/joystick-game-loop` |
| BASIC token/control-code behavior | `c64://examples/basic/mcp-c64-token-test` |
| Larger game references | `c64://examples/c64-ai-toolchain-catalog` |

## C64AIToolChain-Derived Knowledge

The server embeds compact examples and source-derived notes from the MIT-licensed C64AIToolChain review. Larger games and cc65 projects are not referenced by host file path; agents should use the embedded summaries and examples below.

| Area | Embedded starting point | Notes |
| --- | --- | --- |
| Assembly game loops | `c64://examples/assembly/joystick-game-loop` | Useful for gameplay loops, zero-page state, joystick handling, and screen RAM updates. |
| Assembly visual effects | `c64://examples/assembly/raster-bars-demo` | Useful for frame pacing, colour effects, and RUN/STOP-friendly loops. |
| Sprite movement | `c64://examples/assembly/sprite-demo` | Useful for sprite pointers, copied sprite data, movement, and cleanup. |
| cc65-style effects | `c64://examples/c64-ai-toolchain-catalog` | Algorithm notes for starfields, raster bars, fire, plasma, and scrollers; translate to BASIC or supported assembly before compiling. |
| cc65-style games | `c64://examples/c64-ai-toolchain-catalog` | Design notes for collision, scoring, levels, waves, and sound; reference-only until a C compiler workflow exists. |

## Import Policy

- Keep resources self-contained under `Resources/data`; do not expose host-specific absolute paths.
- Keep examples small enough to be read in one resource call where possible.
- Prefer working BASIC or single-file assembly examples before larger cc65-derived notes.
- Mark cc65-derived material clearly as reference-only until a C compiler workflow exists.
- Preserve source attribution and license notes in metadata without requiring access to the original local checkout.

# Citations

[1] Source family: mcp-c64 example sources, adapted into embedded `c64://examples/...` resources.

[2] Source family: MIT-licensed C64AIToolChain examples, summarized into embedded MCP resources.

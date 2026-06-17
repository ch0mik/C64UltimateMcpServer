---
type: Reference
title: Classic C64 Reference Source Catalog
description: Local source catalog for classic C64 books and manuals used to curate MCP resources.
resource: c64://sources/classic-c64-references
tags:
- sources
- reference
- okf
---

# Classic C64 Reference Source Catalog

This resource records local source material that can inform curated MCP resources. These files are large transcriptions or PDFs, so they should be treated as source material rather than embedded wholesale into the MCP resource catalog.

## Local Source Set

| Source | Local file | Best use |
| --- | --- | --- |
| Machine Language for the Commodore 64 and Other Commodore Computers | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\butterfield-ml-for-commodore-computers.txt` | Assembly learning notes, KERNAL calls, monitor/debugging workflow, BASIC and machine-language integration. |
| Commodore 64 Programmer's Reference Guide | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\commodore-64-programmers-reference.txt` | BASIC, graphics, sound, KERNAL routines, memory map, chip specifications, screen and character-code references. |
| Mapping the Commodore 64 | `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\leemon-mapping-the-c64.txt` | Practical memory-location notes, zero page and system variable behavior, I/O area details, screen/color tables, keycodes. |

## Resource Policy

- Do not copy full book/manual transcriptions into `Resources/data`.
- Prefer compact OKF documents with frontmatter, tables, examples, and citations.
- Use paraphrased technical notes unless a short quote is needed.
- Route extracted knowledge into the most specific category: `assembly`, `api`, `memory`, `graphics`, `sound`, `io`, or `drive`.
- Keep this `sources` category as provenance and routing metadata, not as the primary place for technical instructions.

## Candidate Follow-Ups

- `c64://assembly/ml-kernal-quickstart` for CHROUT, GETIN, STOP, SYS loaders, and safe machine-language placement.
- `c64://memory/mapping-notes` for practical memory-location behavior not already captured in the compact memory maps.
- Sprite workflow notes in `c64://graphics/sprite-charset-best-practices`.
- Short usage notes in `c64://api/kernal-api` for common KERNAL calls.

# Citations

[1] Local TXT source: `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\butterfield-ml-for-commodore-computers.txt`

[2] Local TXT source: `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\commodore-64-programmers-reference.txt`

[3] Local TXT source: `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\leemon-mapping-the-c64.txt`

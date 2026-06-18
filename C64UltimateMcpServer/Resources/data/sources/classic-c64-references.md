---
type: Reference
title: Classic C64 Reference Provenance
description: Provenance notes for classic C64 books and manuals used to curate embedded MCP resources.
resource: c64://sources/classic-c64-references
tags:
- sources
- reference
- okf
---

# Classic C64 Reference Provenance

This resource records source provenance for curated MCP resources. The original books/manuals are large reference works, so this MCP server embeds compact, task-oriented notes instead of requiring access to host-specific source files.

## Source Set

| Source | Embedded use |
| --- | --- | --- |
| Machine Language for the Commodore 64 and Other Commodore Computers | Assembly learning notes, KERNAL calls, monitor/debugging workflow, BASIC and machine-language integration. |
| Commodore 64 Programmer's Reference Guide | BASIC, graphics, sound, KERNAL routines, memory map, chip specifications, screen and character-code references. |
| Mapping the Commodore 64 | Practical memory-location notes, zero page and system variable behavior, I/O area details, screen/color tables, keycodes. |

## Resource Policy

- Do not require host-specific absolute paths from MCP resources.
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

[1] Machine Language for the Commodore 64 and Other Commodore Computers.

[2] Commodore 64 Programmer's Reference Guide.

[3] Mapping the Commodore 64.

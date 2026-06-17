---
type: Reference
title: 6510 Assembly Tooling Notes
description: Notes about assembly tooling and the source layout supported by ultimate_generate_assembly_prg.
resource: c64://assembly/tooling-notes
tags:
- assembly
- tooling
---

# 6510 Assembly PRG Tooling Notes

This MCP server exposes `ultimate_generate_assembly_prg` for compiling textual 6510 assembly into a C64 PRG.

## Supported source format

- Origin directives: `.org $C000` or `*= $C000`
- Labels: `label:`
- Instructions: standard 6502/6510 mnemonics and addressing modes
- Data: `.byte ...`, `.word ...`
- Expressions: constants/symbols with `+` and `-`
- Number formats:
  - Hex: `$C000` or `0xC000`
  - Binary: `%10101010`
  - Decimal: `49152`
- Comments: `; ...`

## Not supported in this tool

- Macro systems like `!source`, `!bin`, `+start_at`, `+draw_text`
- External file includes

Use the built-in generator examples under `c64://assembly/examples/*` as the primary reference set.

## Launching compiled PRG

- If `basicRunLoader=false` (default), start program with `SYS <origin>`
- If `basicRunLoader=true`, start with `RUN` (tool prepends BASIC line `10 SYS<origin>`)


---
type: Reference
title: C64 Practical Memory Mapping Notes
description: Practical notes for using C64 memory locations safely from BASIC and 6510 assembly.
resource: c64://memory/mapping-notes
tags:
- memory
- mapping
- reference
- safety
---

# C64 Practical Memory Mapping Notes

Use this resource when a task needs advice about where to place code/data, how to reason about ROM/RAM banking, or which memory locations are practical rather than merely documented.

## Placement Choices

| Range | Use | Notes |
| --- | --- | --- |
| `$0801+` | BASIC program text | Normal BASIC load address. Avoid overwriting it unless generating a complete PRG with a BASIC loader. |
| `$033C-$03FB` | Cassette buffer | Useful for tiny machine-language helpers when tape is not used. Keep the routine small and document that cassette use is incompatible. |
| `$0400-$07E7` | Default screen RAM | Direct writes require screen codes, not PETSCII. |
| `$07F8-$07FF` | Sprite pointers for default screen | Values are 64-byte block numbers within the active VIC bank. |
| `$C000-$CFFF` | Common generated ML workspace | Good default for small SYS-callable routines while BASIC remains active. |
| `$D000-$DFFF` | I/O or character ROM window | Controlled by `$0001` bit 2. Do not assume the same bytes are visible to CPU and VIC-II. |

## Zero Page And System Variables

- `$0000/$0001` are the 6510 data-direction and I/O port registers. Bit 0 controls LORAM, bit 1 controls HIRAM, and bit 2 controls CHAREN.
- `$0090` is the KERNAL status byte (`ST` in BASIC). Device I/O can update it.
- `$00FB-$00FE` are common scratch locations for small machine-language routines, but still document ownership if used.
- BASIC, KERNAL, and interrupt handlers share low memory. Generated examples should avoid low memory unless the purpose is explicitly low-level.

## ROM/RAM Banking Rules

- Keep KERNAL ROM visible when calling KERNAL entry points such as `$FFD2`, `$FFE4`, or `$FFE1`.
- Disable interrupts before temporarily switching out KERNAL ROM for advanced ROM/RAM inspection.
- The CPU and VIC-II can observe memory through different banking rules. Screen, charset, bitmap, and sprite data must be valid from the VIC-II bank being displayed.
- When reading character ROM through the CPU at `$D000-$DFFF`, restore I/O visibility before returning to normal code.

## BASIC Interop Tips

- If BASIC calls a routine with `SYS`, end with `RTS` unless the routine intentionally takes over the machine.
- Preserve registers only when the caller expects it; many short examples freely modify `A`, `X`, and `Y`.
- Keep long loops responsive with KERNAL `STOP` (`$FFE1`) while developing generated demos.
- Prefer high-memory code plus an MCP-generated BASIC loader over hand-building a loader at `$0801`.

# Citations

[1] Source-derived notes from Mapping the Commodore 64.

[2] Related MCP resource: `c64://memory/map`

[3] Related MCP resource: `c64://memory/low`

---
type: Reference
title: Machine Language KERNAL Quickstart
description: Practical 6510 assembly quickstart for SYS loaders, screen output, keyboard polling, and RUN/STOP handling.
resource: c64://assembly/ml-kernal-quickstart
tags:
- assembly
- kernal
- quickstart
- sys
---

# Machine Language KERNAL Quickstart

Use this resource when generating small C64 machine-language helpers that should be callable from BASIC or runnable through `ultimate_generate_assembly_prg`.

## Built-In Generator Shape

The local assembly generator behind `ultimate_generate_assembly_prg` supports a compact source format:

```asm
.org $C000

start:
    lda #$01
    sta $D020
    rts
```

When using `ultimate_generate_assembly_prg`, prefer `basicRunLoader=true` for programs at high memory such as `$C000`. The tool prepends a BASIC `SYS` loader and keeps the generated PRG load address at `$0801`.

## Common KERNAL Calls

| Routine | Address | Input | Output | Use |
| --- | --- | --- | --- | --- |
| `CHROUT` | `$FFD2` | `A` = character/control code | Registers may change | Output one byte to current output device, normally the screen. |
| `GETIN` | `$FFE4` | None | `A` = byte or `0` if no key | Poll keyboard/current input without waiting for a full line. |
| `STOP` | `$FFE1` | None | `Z` set when RUN/STOP is pressed | Let long loops honor RUN/STOP during development. |
| `CLRCHN` | `$FFCC` | None | None | Restore default input/output channels. |
| `SCINIT` | `$FF81` | None | None | Reinitialize the screen editor. |

## Screen Output Loop

```asm
.org $C000

start:
    jsr $FFCC
    ldy #$00
loop:
    lda message,y
    beq done
    jsr $FFD2
    iny
    jmp loop
done:
    rts

message:
    .byte $48,$45,$4C,$4C,$4F,$00
```

`CHROUT` expects PETSCII/control-code bytes. For direct screen RAM writes at `$0400`, use screen codes instead.

## Keyboard Polling Loop

```asm
.org $C000

start:
    jsr $FFE4
    beq start
    jsr $FFD2
    rts
```

`GETIN` returns immediately. A zero byte means no key is available.

## RUN/STOP-Friendly Loop

```asm
.org $C000

loop:
    jsr $FFE1
    beq done
    inc $D020
    jmp loop
done:
    rts
```

This is useful for visual demos or generated experiments that may otherwise run forever.

## Safe Placement Notes

- `$C000-$CFFF` is a practical location for small generated routines when BASIC is active.
- `$033C-$03FB` is the cassette buffer and can hold tiny routines only when cassette/tape use is irrelevant.
- Avoid overwriting `$0801` BASIC text unless the whole PRG is intentionally a machine-code loader or uses the MCP `basicRunLoader` option.
- Avoid switching out KERNAL ROM while relying on KERNAL calls or active interrupts.

# Citations

[1] Local source notes: `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\butterfield-ml-for-commodore-computers.txt`

[2] Local reference notes: `C:\Retro\Commodore\c64-voice-controll\mcp-c64\docs\txt\commodore-64-programmers-reference.txt`

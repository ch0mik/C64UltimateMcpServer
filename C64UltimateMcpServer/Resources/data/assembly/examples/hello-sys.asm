; MCP-compatible 6510 sample (no macros/includes)
.org $C000

start:
    lda #$01
    sta $D020
    lda #$00
    sta $D021
    lda #$48 ; H
    sta $0400
    lda #$49 ; I
    sta $0401
    rts

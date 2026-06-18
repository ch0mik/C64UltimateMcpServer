; Adaptation of an upstream C64 assembly hello-world example for the built-in assembly PRG generator.
; Source family: C64 assembly hello world example.
;
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
.org $C000

start:
    jsr $FFCC
    jsr $FF81
    lda #$00
    sta $D020
    sta $D021
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
    .byte $48,$45,$4C,$4C,$4F,$20,$57,$4F,$52,$4C,$44,$00

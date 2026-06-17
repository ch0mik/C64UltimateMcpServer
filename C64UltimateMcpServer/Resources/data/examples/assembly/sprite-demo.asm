; Single sprite setup demo for the built-in assembly PRG generator.
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
; Copies sprite data to cassette buffer sprite block #13 at $0340.
.org $C000

start:
    lda #$00
    sta $D020
    sta $D021
    ldx #$00

copy_sprite:
    lda sprite_data,x
    sta $0340,x
    inx
    cpx #$3F
    bne copy_sprite

    lda #$0D
    sta $07F8
    lda #$90
    sta $D000
    lda #$80
    sta $D001
    lda #$01
    sta $D027
    sta $D015

loop:
    jsr $FFE1
    beq done
    inc $D000
    jmp loop

done:
    lda #$00
    sta $D015
    rts

sprite_data:
    .byte $00,$18,$00
    .byte $00,$3C,$00
    .byte $00,$7E,$00
    .byte $00,$FF,$00
    .byte $01,$FF,$80
    .byte $03,$FF,$C0
    .byte $07,$FF,$E0
    .byte $0F,$FF,$F0
    .byte $1F,$FF,$F8
    .byte $3F,$FF,$FC
    .byte $7F,$FF,$FE
    .byte $3F,$FF,$FC
    .byte $1F,$FF,$F8
    .byte $0F,$FF,$F0
    .byte $07,$FF,$E0
    .byte $03,$FF,$C0
    .byte $01,$FF,$80
    .byte $00,$FF,$00
    .byte $00,$7E,$00
    .byte $00,$3C,$00
    .byte $00,$18,$00

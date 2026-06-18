; Dreadline-derived row fastscroll adapted for the built-in assembly PRG generator.
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
; Scrolls C64 screen RAM and color RAM rows 4..22 one character left.
; Press RUN/STOP to return.
.org $C000

start:
    lda #$00
    sta $D020
    sta $D021
    jsr seed_right_column

main_loop:
    jsr $FFE1
    beq done
    jsr wait_frame
    jsr scroll_rows
    jsr seed_right_column
    jmp main_loop

done:
    rts

wait_frame:
    lda $D012
wait_change:
    cmp $D012
    beq wait_change
    rts

seed_right_column:
    ldx #$00
seed_loop:
    lda #$51
    sta $04C7,x
    lda #$01
    sta $D8C7,x
    txa
    clc
    adc #$28
    tax
    cpx #$F0
    bne seed_loop
    rts

scroll_rows:
    ldx #$00
row04_loop:
    lda $04A1,x
    sta $04A0,x
    lda $D8A1,x
    sta $D8A0,x
    inx
    cpx #$27
    bne row04_loop

    ldx #$00
row05_loop:
    lda $04C9,x
    sta $04C8,x
    lda $D8C9,x
    sta $D8C8,x
    inx
    cpx #$27
    bne row05_loop

    ldx #$00
row06_loop:
    lda $04F1,x
    sta $04F0,x
    lda $D8F1,x
    sta $D8F0,x
    inx
    cpx #$27
    bne row06_loop

    ldx #$00
row07_loop:
    lda $0519,x
    sta $0518,x
    lda $D919,x
    sta $D918,x
    inx
    cpx #$27
    bne row07_loop

    ldx #$00
row08_loop:
    lda $0541,x
    sta $0540,x
    lda $D941,x
    sta $D940,x
    inx
    cpx #$27
    bne row08_loop

    ldx #$00
row09_loop:
    lda $0569,x
    sta $0568,x
    lda $D969,x
    sta $D968,x
    inx
    cpx #$27
    bne row09_loop

    ldx #$00
row10_loop:
    lda $0591,x
    sta $0590,x
    lda $D991,x
    sta $D990,x
    inx
    cpx #$27
    bne row10_loop

    ldx #$00
row11_loop:
    lda $05B9,x
    sta $05B8,x
    lda $D9B9,x
    sta $D9B8,x
    inx
    cpx #$27
    bne row11_loop

    ldx #$00
row12_loop:
    lda $05E1,x
    sta $05E0,x
    lda $D9E1,x
    sta $D9E0,x
    inx
    cpx #$27
    bne row12_loop

    ldx #$00
row13_loop:
    lda $0609,x
    sta $0608,x
    lda $DA09,x
    sta $DA08,x
    inx
    cpx #$27
    bne row13_loop

    ldx #$00
row14_loop:
    lda $0631,x
    sta $0630,x
    lda $DA31,x
    sta $DA30,x
    inx
    cpx #$27
    bne row14_loop

    ldx #$00
row15_loop:
    lda $0659,x
    sta $0658,x
    lda $DA59,x
    sta $DA58,x
    inx
    cpx #$27
    bne row15_loop

    ldx #$00
row16_loop:
    lda $0681,x
    sta $0680,x
    lda $DA81,x
    sta $DA80,x
    inx
    cpx #$27
    bne row16_loop

    ldx #$00
row17_loop:
    lda $06A9,x
    sta $06A8,x
    lda $DAA9,x
    sta $DAA8,x
    inx
    cpx #$27
    bne row17_loop

    ldx #$00
row18_loop:
    lda $06D1,x
    sta $06D0,x
    lda $DAD1,x
    sta $DAD0,x
    inx
    cpx #$27
    bne row18_loop

    ldx #$00
row19_loop:
    lda $06F9,x
    sta $06F8,x
    lda $DAF9,x
    sta $DAF8,x
    inx
    cpx #$27
    bne row19_loop

    ldx #$00
row20_loop:
    lda $0721,x
    sta $0720,x
    lda $DB21,x
    sta $DB20,x
    inx
    cpx #$27
    bne row20_loop

    ldx #$00
row21_loop:
    lda $0749,x
    sta $0748,x
    lda $DB49,x
    sta $DB48,x
    inx
    cpx #$27
    bne row21_loop

    ldx #$00
row22_loop:
    lda $0771,x
    sta $0770,x
    lda $DB71,x
    sta $DB70,x
    inx
    cpx #$27
    bne row22_loop
    rts

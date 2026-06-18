; Upstream C64 fire/line-demo idea adapted for the built-in assembly PRG generator.
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
; Sets bitmap mode, clears bitmap/screen/color memory, then animates bytes in
; bitmap RAM as a compact HIRES dot-burst reference without include directives.
; Press RUN/STOP to return.
.org $C000

start:
    lda #$00
    sta $D020
    sta $D021
    sta $FB

    lda #$3B
    sta $D011
    lda #$18
    sta $D018

    jsr clear_bitmap
    jsr init_screen_and_color

main_loop:
    jsr $FFE1
    beq done
    jsr wait_frame
    jsr plot_step
    jmp main_loop

done:
    lda #$1B
    sta $D011
    lda #$15
    sta $D018
    lda #$00
    sta $D020
    sta $D021
    rts

wait_frame:
    lda $D012
wait_change:
    cmp $D012
    beq wait_change
    rts

clear_bitmap:
    ldx #$00
    lda #$00
clear_bitmap_loop:
    sta $2000,x
    sta $2100,x
    sta $2200,x
    sta $2300,x
    sta $2400,x
    sta $2500,x
    sta $2600,x
    sta $2700,x
    sta $2800,x
    sta $2900,x
    sta $2A00,x
    sta $2B00,x
    sta $2C00,x
    sta $2D00,x
    sta $2E00,x
    sta $2F00,x
    inx
    bne clear_bitmap_loop
    rts

init_screen_and_color:
    ldx #$00
screen_loop:
    lda #$10
    sta $0400,x
    sta $0500,x
    sta $0600,x
    sta $0700,x
    lda #$01
    sta $D800,x
    sta $D900,x
    sta $DA00,x
    sta $DB00,x
    inx
    bne screen_loop
    rts

plot_step:
    ldx $FB
    lda #$00
    sta $2000,x
    sta $2100,x
    sta $2200,x
    sta $2300,x

    inc $FB
    ldx $FB
    lda #$FF
    sta $2000,x
    sta $2100,x
    sta $2200,x
    sta $2300,x

    lda $FB
    and #$07
    tax
    lda color_table,x
    sta $D020
    rts

color_table:
    .byte $01,$07,$0A,$08,$02,$09,$05,$0D

; Raster colour bars demo for the built-in assembly PRG generator.
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
; Press RUN/STOP to return.
.org $C000

start:
    lda #$00
    sta $D020
    sta $D021

loop:
    jsr $FFE1
    beq done
    jsr wait_frame
    ldx #$00

bars:
    txa
    and #$0F
    sta $D020
    sta $D021
    jsr short_delay
    inx
    cpx #$40
    bne bars
    jmp loop

done:
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

short_delay:
    ldy #$20
delay_loop:
    dey
    bne delay_loop
    rts

; Joystick game-loop skeleton for the built-in assembly PRG generator.
; Use with ultimate_generate_assembly_prg and basicRunLoader=true.
; Joystick port 2 moves a marker left/right on the top screen row.
.org $C000

start:
    lda #$00
    sta $D020
    sta $D021
    lda #$14
    sta $FB

loop:
    jsr $FFE1
    beq done
    jsr wait_frame

    ldx $FB
    lda #$20
    sta $0400,x

    lda $DC00
    and #$04
    beq move_left

    lda $DC00
    and #$08
    beq move_right
    jmp draw

move_left:
    lda $FB
    beq draw
    dec $FB
    jmp draw

move_right:
    lda $FB
    cmp #$27
    beq draw
    inc $FB

draw:
    ldx $FB
    lda #$51
    sta $0400,x
    jmp loop

done:
    rts

wait_frame:
    lda $D012
wait_change:
    cmp $D012
    beq wait_change
    rts

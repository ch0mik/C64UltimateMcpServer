; MCP-compatible 6510 text scroller
; Inspired by the classic VIC-II hardscroll + softscroll pattern from Codebase64 text scrolling notes.
; Strategy:
; - softscroll with $D016 lower 3 bits (7..0)
; - when softscroll wraps, perform one-char hardscroll in screen RAM row 0
; - append next character from message stream

.org $1000

start:
    ; simple colors
    lda #$00
    sta $d020
    lda #$06
    sta $d021

    ; clear first text row to spaces
    ldx #$00
    lda #$20
clear_row:
    sta $0400,x
    inx
    cpx #$28
    bne clear_row

    ; preload first 40 chars
    ldx #$00
seed_row:
    lda message,x
    sta $0400,x
    inx
    cpx #$28
    bne seed_row

    ; next char after seed window
    lda #$28
    sta msg_index

main_loop:
    jsr wait_frame
    jsr scroll_step
    jmp main_loop

wait_frame:
    ldy #$40
wait_outer:
    ldx #$ff
wait_inner:
    dex
    bne wait_inner
    dey
    bne wait_outer
    rts

scroll_step:
    lda scroll_x
    sec
    sbc #$01
    and #$07
    sta scroll_x

    lda $d016
    and #$f8
    ora scroll_x
    sta $d016

    ; wrap point (0->7): do hardscroll
    lda scroll_x
    cmp #$07
    bne scroll_done
    jsr hardscroll_row0

scroll_done:
    rts

hardscroll_row0:
    ldx #$00
shift_loop:
    lda $0401,x
    sta $0400,x
    inx
    cpx #$27
    bne shift_loop

    ldx msg_index
    lda message,x
    sta $0427

    inx
    cpx #$60
    bne store_index
    ldx #$00
store_index:
    stx msg_index
    rts

scroll_x:
    .byte $07
msg_index:
    .byte $00

; Screen codes (A=1..Z=26, space=$20)
message:
    .byte $03,$0f,$04,$05,$02,$01,$13,$05,$36,$34,$20,$14,$05,$18,$14,$20
    .byte $13,$03,$12,$0f,$0c,$0c,$20,$04,$05,$0d,$0f,$20,$20,$20,$03,$0f
    .byte $0d,$0d,$0f,$04,$0f,$12,$05,$20,$36,$34,$20,$0d,$03,$10,$20,$20
    .byte $13,$0f,$06,$14,$13,$03,$12,$0f,$0c,$0c,$20,$08,$01,$12,$04,$20
    .byte $03,$0f,$04,$05,$02,$01,$13,$05,$36,$34,$20,$13,$14,$19,$0c,$05
    .byte $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20

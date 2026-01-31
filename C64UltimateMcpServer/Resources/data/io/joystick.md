# Commodore 64 Joystick Reference

## Quick Summary
- **Port 1 Address:** PEEK(56321) or $DC01
- **Port 2 Address:** PEEK(56320) or $DC00
- **Bits are Active-Low:** 0 = pressed, 1 = idle
- **Port 1 Idle:** 255
- **Port 2 Idle:** 127 (bits 7,6 are always set)

## Bit Mapping (Active-Low)
| Bit | Weight | Direction/Button |
|-----|--------|-----------------|
| 0   | 1      | UP              |
| 1   | 2      | DOWN            |
| 2   | 4      | LEFT            |
| 3   | 8      | RIGHT           |
| 4   | 16     | FIRE            |

## BASIC Example
```basic
10 JV=PEEK(56320)    : REM Read Port 2
20 IF (JV AND 1)=0 THEN PRINT "UP"
30 IF (JV AND 2)=0 THEN PRINT "DOWN"
40 IF (JV AND 4)=0 THEN PRINT "LEFT"
50 IF (JV AND 8)=0 THEN PRINT "RIGHT"
60 IF (JV AND 16)=0 THEN PRINT "FIRE"
70 GOTO 10
```

## Important Notes
- **Port 1 shares keyboard matrix:** Joystick movement can trigger key presses
- **Port 2 is safer for games:** Separate from keyboard
- **Disable keyboard if needed:** POKE 56322,224 (disable), POKE 56322,255 (enable)
- **Bits must be tested individually:** Use AND operation for each direction

## Reference Values
**No input:** Port 1=255, Port 2=127
**UP only:** Port 1=254, Port 2=126
**DOWN only:** Port 1=253, Port 2=125
**LEFT only:** Port 1=251, Port 2=123
**RIGHT only:** Port 1=247, Port 2=119

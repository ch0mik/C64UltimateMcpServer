!- TIC-TAC-TOE-64
!- Ported from a QB64 version,
!- which was written based on several sources
!-
!- v 0.1 - 3 June 2021
!- v 0.3 - 7 June 2021
!-              To correct spelling/print typos
!-              To add/use sprites for "marker"
!-------------------------------------------------------------------------------
!-
!-** REGISTERS **
!-CONST BORDER 53280
!-CONST BGROUND 53281
!-CONST COLRAM 55296
!-
!-** Positions For Text Row And Column When Using Locate
!-CONST TEXTROW 20
!-CONST TEXTCOLUMN 26
!-
!----------------------------------
10 rem print"{home}";: gosub 5000
20 rem gosub 2000
30 rem for i=0 to 9
40 rem    if sx(i)<255 then poke v,sx(i): goto 60
50 rem    poke v+16,1 : poke v,sx(i)-255
60 rem    poke v+1,sy(i)
70 rem for t=0 to 999: next
80 rem next
90 rem end
!----------------------------------
!- Game variables/settings
100 ro$="{home}{down*24}"
110 co$="{right*39}"
120 compu% = 1
130 human% = 2
140 dim board$(3,3): xo=2: xs=9: yo=3: ys=8
!-
!- Computer will move with 'o', and human with 'x'
150 nought$ = "o"
160 cross$ = "x"
!-
170 gosub 2050 : rem setup sprite data
!---------------------------------------------------------------------------------------------------
!-   Main program - tic tac toe
!---------------------------------------------------------------------------------------------------
180 print "{light gray}{clear}"
190 poke BGROUND,pkBlue : poke BORDER,PkBlue
200 gosub 600 : rem initialize board
210 gosub 660 : rem show board
220 a$="do you want to move first?"
230 gosub 1910 : rem get Y/N
240 if k$="y" then pl%=human%: goto 260
250 if k$="n" then pl%=compu%: goto 260
260 gosub 320: rem main game routine
270 a$="do you want to play again?"
280 gosub 1910 : rem get Y/N
290 if k$="y" then 180
300 print: print "bye."
310 end
!---------------------------------------------------------------------------------------------------
!-  The tic-tac-toe game routine
!-
!-
320 print"{clear}" : gosub 660: rem show board
330 mv = 0 : ov = 0
!-  Do...
340   if pl%<>compu% then 440
!-    Computer's move
350     gosub 1200
360     r=int(m/3): c=m-3*r
370     bo$(r,c)=nought$
380     gosub 660: rem show board
390     print left$(ro$,20);left$(co$,29);"  computer";
400     print left$(ro$,21);left$(co$,29);"  put o in";
410     print left$(ro$,22);left$(co$,29);"   cell"; m+1;
415     print left$(ro$,23);left$(co$,29);"         ";
420     for t=1 to 1000 : next
430     goto 520
!-    Human's move
440     gosub 940 : gosub 1690: rem get user input
450     r=int(m/3): c=m-3*r
460     bo$(r,c)=cross$
470     gosub 660: rem show board
480     print left$(ro$,20);left$(co$,29);"  you put ";
490     print left$(ro$,21);left$(co$,29);"    x in  ";
500     print left$(ro$,22);left$(co$,29);"   cell"; m+1;
505     print left$(ro$,23);left$(co$,29);"         ";
510     for t=1 to 1000 : next
520   gosub 990 :rem check if game is over
530   if ov=1 then 580 : rem game over, exit loop
540   mv=mv+1
550   if mv=9 then 580 : rem no more move, exit loop
560   if pl%=compu% then pl%=human% : goto 340
570   pl%=compu% : goto 340
!-  Loop
580 gosub 1110 :rem Declare winner
590 return
!-
!-
!---------------------------------------------------------------------------------------------------
!- Initialise board
!-
600 for r=0 to 2
610   for c=0 to 2
620     board$(r,c)=" "
630   next
640 next
650 return
!-
!---------------------------------------------------------------------------------------------------
!- Subroutine to show board + contents
!-
660 print "{light gray}{home}";
670 gosub 2250 :rem print empty board
680 x=xo : y=yo
690 x2=xo+xs*3: y2=yo+ys*3
!-  Draw symbols or put cell numbers
700 for i=0 to 2
710   for j=0 to 2
720     if board$(i,j)<>nought$ then 810
730       x = xo + xs*j
740       y = yo + ys*i
750       print left$(ro$,y);left$(co$,x);"U{C*4}I"
760       print left$(ro$,y+1);left$(co$,x);"B{space*4}B"
770       print left$(ro$,y+2);left$(co$,x);"B{space*4}B"
780       print left$(ro$,y+3);left$(co$,x);"B{space*4}B"
790       print left$(ro$,y+4);left$(co$,x);"J{C*4}K"
800       goto 910
810     if board$(i,j)<>cross$ then 900
820       x = xo + xs*j
830       y = yo + ys*i
840       print left$(ro$,y);left$(co$,x);"M   N"
850       print left$(ro$,y+1);left$(co$,x);" M N"
860       print left$(ro$,y+2);left$(co$,x);"  V"
870       print left$(ro$,y+3);left$(co$,x);" N M"
880       print left$(ro$,y+4);left$(co$,x);"N   M"
890       goto 910
900     print left$(ro$,i*ys+yo+2);left$(co$,j*xs+xo+2);i*3+j+1
910   next
920 next
930 return
!---------------------------------------------------------------------------------------------------
!-  Subroutine to show the instructions
!-
940 print"{cyan}"
950 print left$(ro$,20);left$(co$,29);"  select  ";
960 print left$(ro$,21);left$(co$,29);" your move";
970 print left$(ro$,22);left$(co$,29);"          ";
975 print left$(ro$,23);left$(co$,29);"  1 - 9  ";
980 return
!-
!---------------------------------------------------------------------------------------------------
!- Subroutine to check if game is over
!- Set over=1 if game over, esle over=0
!-
!-   Check rows
990 ov = 0
1000 for r=0 to 2
1010   if bo$(r,0)=bo$(r,1) and bo$(r,1)=bo$(r,2) and bo$(r,0)<>" " then ov=1
1020 next
1030 if ov=1 then return
!-   Check columns
1040 for c=0 to 2
1050   if bo$(0,c)=bo$(1,c) and bo$(1,c)=bo$(2,c) and bo$(0,c)<>" " then ov=1
1060 next
1070 if ov=1 then return
!-   Check diagonals
1080 if bo$(0,0)=bo$(1,1) and bo$(1,1)=bo$(2,2) and bo$(0,0)<>" " then ov=1
1090 if bo$(0,2)=bo$(1,1) and bo$(1,1)=bo$(2,0) and bo$(0,2)<>" " then ov=1
1100 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to declare the winner
!-
1110 if ov=0 and mv=9 then a$=" it's a draw   ": goto 1140
1120 if pl% = compu% then a$="computer wins!"
1130 if pl% = human% then a$= "   you win!"
1140 print left$(ro$,8);left$(co$,3);"{yellow}{cm a}{C*20}{cm s}"
1150 print left$(ro$,9);left$(co$,3);"B{space*20}B"
1160 print left$(ro$,9);left$(co$,7);a$;
1170 print left$(ro$,10);left$(co$,3);"{cm z}{C*20}{cm x}"
1180 for t=1 to 1000 : next
1190 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to compute computer's move
!-
!-   check for winning moves
1200 sy$ = nought$
1210 gosub 1400: rem check for winning rows
1220 if m > -1 then return
1230 gosub 1520: rem check for winning columns
1240 if m > -1 then return
1250 gosub 1640: rem check for winning diagonals
1260 if m > -1 then return
!-
!-   check for blocking moves
1270 sy$ = cross$
1280 gosub 1400: rem check rows if need to block
1290 if m > -1 then return
1300 gosub 1520: rem check for columns if need to block
1310 if m > -1 then return
1320 gosub 1640: rem check for diagonals if need to block
1330 if m > -1 then return
!-
!-   else, check middle cell if empty
1340 if bo$(1,1)=" " then m = 4 : return
!-   else, move to a random cell
1360 m = int(rnd(0)*9)
1370 r = int(m/3) : c = m - r*3
1380 if bo$(r,c)<>" " then 1360
1390 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to check rows for a winning move or to block opponent's move
!-
1400 m = -1
1410 for r=0 to 2
1420   if bo$(r,0)<>sy$ then 1470
1430     if bo$(r,1)<>sy$ or bo$(r,2)<>" " then 1450
1440       m = r*3 + 2 : return
1450       if bo$(r,2)<>sy$ or bo$(r,1)<>" " then 1470
1460         m = r*3 + 1 : return
1470   if bo$(r,1)<>sy$ then 1500
1480     if bo$(r,2)<>sy$ or bo$(r,0)<>" " then 1500
1490         m = r*3 : return
1500 next
1510 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to check columns for a winning move or to block opponent's move
!-
1520 m = -1
1530 for c=0 to 2
1540   if bo$(0,c)<>sy$ then 1590
1550     if bo$(1,c)<>sy$ or bo$(2,c)<>" " then 1570
1560       m = 2*3 + c : return
1570     if bo$(2,c)<>sy$ or bo$(1,c)<>" " then 1590
1580       m = 3 + c : return
1590   if bo$(1,c)<>sy$ then 1620
1600     if bo$(2,c)<>sy$ or bo$(0,c)<>" " then 1620
1610       m = c : return
1620 next
1630 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to check diagonals for a winning move or to block opponent's move
!-
1640 m = -1
1650 if bo$(1,1)<>sy$ then return
1660 if bo$(0,0)=sy$ and bo$(2,2)=" " then m = 8 : return
1665 if bo$(2,2)=sy$ and bo$(0,0)=" " then m = 0 : return
1670 if bo$(0,2)=sy$ and bo$(2,0)=" " then m = 6 : return
1675 if bo$(2,0)=sy$ and bo$(0,2)=" " then m = 2 : return
1680 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to get user input/move
!-
1690 poke v+21,0 : rem turn off all sprites
1700 poke v+16,1 : poke v,sx(9)-255 : poke v+1,sy(9) : rem put marker at input area
1710 poke v+21,1 : n=0 : rem turn on sprite
1720 get k$: if k$<>"" then 1770
1730   poke v+39,sc(n): n=n+1
1740   if n>5 then n=0
1750   for t=0 to 99 : next
1760 goto 1720
1770 k = asc(k$)-48
1780 if k<1 or k>9 then 1690
1790 m = k-1
1800 r=int(m/3): c=m-3*r
!-   Highlight the selected cell
1810 poke v+16,0 : poke v,sx(m) : poke v+1,sy(m)
1820 for p=0 to 3
1830   poke v+39,sc(n): n=n+1
1840   if n>5 then n=0
1850   for t=0 to 99 : next
1860 next
1870 poke v+21,0 : rem turn off sprite
1880 if bo$(r,c)<>" " then 1690
1890 rem
1900 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to get user Y/N from user
!-
1910 print left$(ro$,15);left$(co$,2);"{white}{cm a}{C*26}{cm s}"
1920 print left$(ro$,16);left$(co$,2);"B{space*26}B"
1930 print left$(ro$,16);left$(co$,3);a$;
1940 print left$(ro$,17);left$(co$,2);"B{space*26}B"
1950 print left$(ro$,17);left$(co$,3);"press y or n...";
1960 print left$(ro$,18);left$(co$,2);"{cm z}{C*26}{cm x}"
1970 get k$ : if k$="" then 1970
1980 if k$="y" then return
1990 if k$="Y" then k$="y": return
2000 if k$="n" then return
2010 if k$="N" then k$="n": return
2020 print left$(ro$,21);"Please enter y or n:";
2030 for t=1 to 100: next
2040 goto 1910
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to setup sprite for target marker
!-
2050 v=53248
2060 for n=0 to 62
2070   read q : poke 832+n,q : rem sprite data in tape buffer
2080 next
2090 poke 2040,13 : rem sprite 0 image -> tape buffer
2100 poke v+39,pkYellow : rem sprite 0 colour
2110 poke v+23,1 : poke v+29,1 : rem expand sprite 0 both direction
2120 poke v+27,1 : rem set all sprites behind text
2130 rem ** Marker coordinates on screen for sprite position
2140 sx(0)=40: sx(1)=sx(0)+72: sx(2)=sx(1)+72
2150 sx(3)=sx(0) : sx(4)=sx(1) : sx(5)=sx(2)
2160 sx(6)=sx(0) : sx(7)=sx(1) : sx(8)=sx(2)
2170 sx(9)= sx(8)+84
2180 sy(0)=66: sy(1)=sy(0): sy(2)=sy(0)
2190 sy(3)=sy(0)+64: sy(4)=sy(3): sy(5)=sy(3)
2200 sy(6)=sy(3)+64: sy(7)=sy(6): sy(8)=sy(6)
2210 sy(9)=sy(8)+24
2220 dim sc(6) : rem Shades of colors for the sprites
2230 sc(0)=pkDarkGrey: sc(1)=pkMedGrey: sc(2)=pkLightGrey: sc(3)=pkWhite: sc(4)=sc(2): sc(5)=sc(1)
2240 return
!-
!---------------------------------------------------------------------------------------------------
!-   Subroutine to print empty board and title
!-
2250 PRINT "{light gray}{176}{C*26}{174}  {reverse on}{pink}{160}{160}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{169}{160}{127}"
2260 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}   {reverse on}{pink}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}"
2270 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}   {reverse on}{pink}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}"
2280 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}   {reverse on}{pink}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}"
2290 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}   {reverse on}{pink}{160}{reverse off}  {reverse on}{160}{reverse off} {127}{reverse on}{160}{reverse off}{169}"
2300 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}"
2310 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125} {reverse on}{cyan}{160}{160}{160}{reverse off} {reverse on}{169}{160}{127}{reverse off} {reverse on}{169}{160}{127}";
2320 PRINT "{reverse off}{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{cyan}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{160}"
2330 PRINT "{light gray}{125}{cyan}{C*8}{123}{C*8}{123}{C*8}{light gray}{125}  {reverse on}{cyan}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{160}"
2340 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{cyan}{160}{reverse off}  {reverse on}{160}{160}{160}{reverse off} {reverse on}{160}"
2350 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{cyan}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {127}{reverse on}{160}{reverse off}{169}";
2360 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}"
2370 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125} {reverse on}{light green}{160}{160}{160}{reverse off} {reverse on}{169}{160}{127}{reverse off} {reverse on}{160}{160}{160}";
2380 PRINT "{reverse off}{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{light green}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{160}"
2390 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{light green}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{160}{160}"
2400 PRINT "{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}  {reverse on}{light green}{160}{reverse off}  {reverse on}{160}{reverse off} {reverse on}{160}{reverse off} {reverse on}{160}"
2410 PRINT "{light gray}{125}{cyan}{C*8}{123}{C*8}{123}{C*8}{light gray}{125}  {reverse on}{light green}{160}{reverse off}  {127}{reverse on}{160}{reverse off}{169} {reverse on}{160}{160}{160}";
2420 PRINT "{reverse off}{light gray}{125}        {cyan}{125}        {125}        {light gray}{125}"
2430 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2440 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2450 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2460 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2470 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2480 PRINT "{125}        {cyan}{125}        {125}        {light gray}{125}"
2490 PRINT "{173}{C*26}{189}";
2500 return

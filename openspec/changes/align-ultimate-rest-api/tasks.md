# Tasks

Plan do późniejszego wdrożenia. Na etapie analizy nie zmieniono kodu ani pakietów i nie wywołano urządzenia.

## 1. Wspólna komunikacja i walidacja

- [ ] 1.1 Wprowadzić opcje klienta i kontrolowaną fabrykę transportu lub wstrzykiwany HttpMessageHandler z zachowaniem poprzedniego konstruktora oraz opcjonalne tokeny; testy blokowanego handlera sprawdzają anulowanie i lifetime transportu. Udokumentować sposób użycia w README biblioteki.
- [ ] 1.2 Dodać konfigurację NetworkPassword i stosowanie X-Password tylko do urządzenia; testy JSON/binary/no-password, zewnętrznego downloadu i przekierowań 301/302/303/307/308 muszą wykazać brak ujawnienia i przekazania sekretu. Na rzeczywistych lokalnych originach sprawdzić brak drugiego żądania i retransmisji komendy; ta sama reguła obowiązuje konstruktor domyślny i DI. Dodać przykład konfiguracji bez prawdziwego hasła.
- [ ] 1.3 Ujednolicić obsługę errors, statusu i odpowiedzi binarnych, zwalnianie zasobów i klasyfikację błędów; testy obejmują 2xx z errors, 403, 404, 501, JSON zamiast binary, nieprawidłowy JSON i poweroff bez potwierdzenia. Zachować istniejące formaty narzędzi, nowe opisać w README.
- [ ] 1.4 Dodać wspólną walidację pamięci, hex, dysków i enumeracji; testy graniczne mają potwierdzić brak HTTP dla błędnego wejścia. Zestaw poprawnych argumentów i limitów udokumentować przy metodach i narzędziach.
- [ ] 1.5 Dodać testy kodowania URL zgodne z parserem firmware: slash, spacja, Unicode i query w ścieżce. Zmienić budowniczy URI tylko przy potwierdzonej różnicy; wynik i ewentualną niejednoznaczność zapisać w raporcie zgodności.

## 2. Informacje urządzenia i transfery

- [ ] 2.1 Dodać GetDeviceInfoAsync i ultimate_get_device_info, z opcjonalnymi polami; testy klienta i MCP obejmują brak unique_id/core_version oraz błąd nieobsługiwanej trasy. Dodać bezpieczny przykład odczytu i opis różnicy wersji REST/firmware.
- [ ] 2.2 Dodać LoadProgramBinaryAsync i ultimate_load_prg_binary; test żądania ma wykazać load_prg zamiast run_prg i niezmienione bajty. Opisać reset przy ładowaniu i brak automatycznego wykonania.
- [ ] 2.3 Wystawić cztery istniejące metody binarne klienta jako narzędzia MOD/CRT/mount/ROM; testy sprawdzają bazę64, parametry, metadane, puste i błędne dane oraz isError. Dodać oddzielne przykłady wymagające urządzenia i poprawić nadmierne deklaracje wsparcia w README.
- [ ] 2.4 Dodać opcjonalny załącznik song-lengths do SID z zachowaniem starej sygnatury; test przechwytuje dwa załączniki i songnr, a obecny test pojedynczego uploadu nadal przechodzi. Udokumentować opcjonalny parametr.

## 3. Menu i wejście

- [ ] 3.1 Dodać GetMenuScreenAsync, dekoder i ultimate_get_menu_screen; fixture sprawdza macierze, kolory, reverse-video, glify, błędną długość oraz 404 bez zmiany menu. Dodać przykład i opis, że odczyt dotyczy menu firmware.
- [ ] 3.2 Dodać modele wejścia, whitelisty i walidację całego finalnego JSON; testy obejmują niezależne limity bajtów/elementów, duplikaty, niepoprawny port, ograniczenia RESTORE, release_all z zakazanymi polami oraz brak częściowego HTTP.
- [ ] 3.3 Dodać klienta GET/POST input i trzy narzędzia stanu/batch/release; testy przechwytują kolejność zdarzeń, transitions, stan wynikowy, 501 i brak fallbacku przez pamięć. Dodać dokumentację oraz przykłady w grupie operacji zmieniających stan urządzenia.

## 4. Integracja i przegląd

- [ ] 4.1 Uaktualnić testy katalogów o jawnie dozwolone nowe narzędzia, zachowując kontrolę parametrów i wyników starych; wykonać pełne testy Release i osiem obecnych przykładów bez fizycznego C64.
- [ ] 4.2 Porównać implementację z czterema specyfikacjami, poprawić tabelę pokrycia REST/MCP i zapisać verification.md z zakresem firmware oraz ograniczeniami. Wykonać OpenSPEC --strict i przegląd diff.
- [ ] 4.3 Zaktualizować README serwera i biblioteki, przykłady MD, specyfikacje OpenSpec oraz RELEASE_NOTES.md na podstawie faktycznie wdrożonych funkcji; niezależny subagent wykonuje review, a końcowa odpowiedź zawiera surowy Markdown release notes do wklejenia. Potwierdzić zgodność opisów z testami i raportem weryfikacji.

## Workflow follow-up

- Po zakończeniu i weryfikacji zarchiwizować zmianę i zsynchronizować specyfikacje główne.
- Testy menu, wejścia i operacji binarnych na fizycznym urządzeniu wykonywać jako oddzielny etap.

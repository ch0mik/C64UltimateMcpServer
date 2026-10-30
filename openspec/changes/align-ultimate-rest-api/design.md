# Design

## Context

Stan: 2026-10-30. Analiza odpowiada na polecenie `/plan`; kod aplikacji i pakiety nie są zmieniane. Punktem odniesienia jest [dokumentacja API](https://1541u-documentation.readthedocs.io/en/latest/api/api_calls.html) oraz lokalny klient i UltimateService. Zestaw ustaleń i zastrzeżeń zawiera audit.md.

Klient obecnie sam tworzy HttpClient. Większość metod używa wspólnych HandleResponse, ale readmem ma osobną obsługę binarną. Błędy nie-2xx gubią tablicę errors, a obiekty odpowiedzi/JsonDocument nie są konsekwentnie zwalniane. Istniejące testy klienta sprawdzają głównie konstrukcję, Dispose i wyjątki, nie mapowanie żądań REST. Testy MCP mają już host aplikacji i bazowe kontrakty.

## Goals / Non-Goals

**Goals:** uzupełnienie rzeczywistych luk REST/MCP, poprawa diagnozowania błędów, testowalny klient oraz podstawy dla wcześniej zaplanowanych Apps/MRTR.

**Non-Goals:** wdrożenie firmware, zmiana adresu urządzenia, automatyczna rekonfiguracja po SetConnection, nowe nieudokumentowane endpointy, emulowanie wejścia przez pamięć, pełny streaming obrazu oraz wykonywanie operacji na C64 podczas tego planowania.

## Decisions

### 1. Priorytety

1. P1: transport, hasło, błędy, walidacja i informacje o urządzeniu.
2. P2: uzupełnienie operacji binarnych i poprawienie dokumentacji pokrycia.
3. P3: odczyt menu i wejście klawiatury/joysticków, z obsługą niedostępności firmware i sprzętu.

Wszystkie etapy stanowią jeden plan, ale każdy dostarcza własne testy i opis zachowania. Nie włączać próbek aktywnie naciskających klawisze ani uruchamiających programy do domyślnego smoke.

### 2. Wspólny transport i hasło

Wprowadzić opcje klienta zawierające BaseUrl, timeout i opcjonalne NetworkPassword. Konfiguracja serwera: `Ultimate:NetworkPassword`, w środowisku `Ultimate__NetworkPassword`; nie dodawać prawdziwego hasła do plików repozytorium. To poświadczenie urządzenia, odrębne od uwierzytelniania klienta MCP.

Zachować obecny konstruktor i umożliwić testowalny transport przez kontrolowaną fabrykę klienta lub wstrzyknięty HttpMessageHandler, z jawną własnością lifetime. Wspólna ścieżka żądań stosuje hasło wyłącznie do urządzenia, przekazuje CancellationToken, zwalnia request/content/response i dokumenty JSON. Pobieranie pliku z podanego URL nie może przejąć nagłówka urządzenia. Secret nie jest parametrem narzędzi ani częścią wyników i logów.

Klient urządzenia ma `AllowAutoRedirect = false`: odpowiedź 30x jest błędem UnexpectedRedirect, bez podążania za Location i bez automatycznej retransmisji komendy. Dotyczy to także przekierowań w obrębie tego samego originu. Niestandardowy X-Password nie korzysta z automatycznej ochrony nagłówka Authorization przy przekierowaniu — zob. [HttpClientHandler.AllowAutoRedirect](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.allowautoredirect?view=net-10.0). Regułę stosować zarówno w domyślnym konstruktorze, jak i rejestracji DI. Nie wprowadzać overloadu przyjmującego dowolny niekontrolowany HttpClient, którego ukrytej polityki przekierowań nie da się sprawdzić. Dostawca wstrzykniętego handlera/fabryki musi przestrzegać tego samego kontraktu. Test transportu na dwóch lokalnych originach sprawdza, że drugi nie otrzyma żądania, a pierwszy dostanie pojedynczą komendę.

Rozszerzyć UltimateClientException o opcjonalny status HTTP i kategorię błędu, pozostawiając obecne konstruktory. Parsować JSON errors także przy nie-2xx. Odróżnić odmowę dostępu, nieobsługiwaną operację, walidację, awarię transportu i nieprawidłową odpowiedź. Nie traktować JSON błędu jako zawartości binarnej. Nie zamieniać OperationCanceledException na zwykły błąd urządzenia.

Nowe narzędzia zwracają CallToolResult z isError i structuredContent według istniejącego helpera. Dotychczasowe narzędzia zwracające string zachowują format; nie przepisywać masowo ich typów wyników. Retryability zależy od statusu i rodzaju operacji. Powtórzenie wywołania mającego skutek uboczny nie następuje automatycznie. Rozłączenie przy poweroff nie jest dowodem ani sukcesu, ani niewykonania: wynik ma oznaczać niepotwierdzone wykonanie.

### 3. Informacje i wsparcie funkcji

Dodać `DeviceInfoResponse` z mapowaniem snake_case i opcjonalnymi polami, klient `GetDeviceInfoAsync` i narzędzie `ultimate_get_device_info`. Zachować osobne GetVersion: wersja REST nie zastępuje wersji firmware. Dane urządzenia będą też dostępne dla planowanego dashboardu.

Nie przypisywać nowych endpointów do konkretnej wersji firmware, jeśli źródło nie podaje minimum. Brak funkcji obsługiwać na podstawie odpowiedzi urządzenia; nie wykonywać prób POST podczas autodetekcji. Sam produkt lub numer wersji nie gwarantuje działającego endpointu.

### 4. Odczyt menu

Nowy klient `GetMenuScreenAsync` i `ultimate_get_menu_screen` korzystają z dedykowanego dekodera. Reprezentacja zawiera wymiary, oba oryginalne bufory, bazową wersję tekstową i atrybuty komórek. Wymiary i długość wynikają z kontraktu opisanego w audit.md. Nie interpretować tych bajtów bezwarunkowo jako UTF-8 ani jako pamięci ekranowej programu C64. Znaków specjalnych nie zastępować bez śladu: zachować ich kody, reverse-video i kolory.

Rozróżnić znany błąd niedostępnego menu od ogólnego braku endpointu. Nie otwierać ani nie zamykać menu przy odczycie, bo istniejący menu_button jest przełącznikiem. Zasób/renderer MCP Apps menu może być później dodany do dashboardu; odczyt REST nie wymaga Apps.

### 5. Zdarzenia wejścia

Dodać typowane modele keyboard/joystick/release_all oraz InputStateResponse. Nazwy narzędzi: `ultimate_get_input_state`, `ultimate_send_input_events(events)` i `ultimate_release_inputs`. Utrwalić whitelisty i ograniczenia w fixture ze wskazaniem wersji dokumentacji; nie kopiować walidacji do kilku metod.

Walidować wszystkie elementy, a następnie serializować finalny JSON do UTF-8 i sprawdzić limit bajtów, zanim cokolwiek zostanie wysłane. Limit elementów nie zastępuje limitu bajtów. Nie dzielić niejawnie odrzuconej partii na mniejsze, bo zmieniłoby to atomowość operacji. Zwracać stan zwrócony przez urządzenie, nie przewidywany stan lokalny.

Tap jest zalecanym sposobem jednorazowego wejścia; press pozostawia wejście wciśnięte aż do release. Jawne release-all może dotyczyć wejść innych klientów tego samego urządzenia, dlatego nie wykonywać go automatycznie po dowolnym rozłączeniu klienta MCP. Nie symulować błędu NotSupported przez zapis bufora klawiatury lub pamięci joysticka.

### 6. Operacje binarne

| Nowe narzędzie MCP | Wykorzystanie klienta |
| --- | --- |
| ultimate_load_prg_binary | Nowa metoda LoadProgramBinaryAsync |
| ultimate_play_mod_binary | Istniejące PlayModBinaryAsync |
| ultimate_run_crt_binary | Istniejące RunCartridgeBinaryAsync |
| ultimate_mount_disk_binary | Istniejące MountDiskBinaryAsync |
| ultimate_load_drive_rom_binary | Istniejące LoadDriveRomBinaryAsync |

Nowe narzędzia przyjmują nazwany parametr base64 zgodny z rodzajem danych, a dodatkowo drive/type/mode tam, gdzie są potrzebne. Nie wymagają tworzenia pliku w filesystem urządzenia. Wspólny helper dekoduje base64, odrzuca puste dane i stosuje limity wejścia. Dla binarnego mount wymagać jawnego type, jeśli nie ma wiarygodnej nazwy pliku pozwalającej określić format. Przenosić metadane załącznika, jeśli są użyte; bajtów nie zmieniać.

SID: opcjonalne `songLengthsDataBase64` i overload klienta; żądanie z drugim plikiem wykorzystuje dwa załączniki, a obecny wariant pozostaje zgodny. Testować kolejność i parametry załączników względem parsera firmware. Nie zmieniać wszystkich dotychczasowych uploadów na multipart bez dowodu, że jest to potrzebne.

Ładowanie PRG bez run nadal może resetować maszynę. Opisy narzędzi i przykłady muszą to jasno komunikować. ROM napędu jest tymczasowy, a zmiana trybu może go zastąpić — opisy nie mogą sugerować trwałego zapisania firmware.

### 7. Walidacja pozostałych parametrów

Jeden parser adresów/hex oraz sprawdzanie zakresu operacji pamięci bez przepełnień arytmetycznych. Sprawdzić udokumentowane limity read/query-write/binary-write. D64 akceptuje przewidziane warianty ścieżek, DNP poprawny zakres, a enumeracje typ/mode/stream są zamknięte do wspieranych wartości. Poprawne dotychczasowe argumenty zachowują znaczenie; niepoprawne kończą się lokalnym błędem.

Wspólny budowniczy URI musi mieć testy ścieżek ze spacjami, Unicode, ukośnikami i znakami query. Zmianę sposobu kodowania `files` wykonać dopiero po teście zachowania parsera firmware. Nie wprowadzać nieudokumentowanego unlink w reakcji na powtórzoną pozycję dokumentacji. Surowa odpowiedź drives już zachowuje SoftIEC, więc modelowanie tego obszaru jest dodatkiem, nie naprawą rzekomej utraty danych.

### 8. Współpraca z planem MCP 2.3

Ten plan jest właścicielem testowalnego transportu, poświadczeń, błędów i tokenów klienta REST. `enable-mcp-2-3-features` wykorzystuje ten rezultat dla Tasks i obserwatora, zamiast implementować drugi transport.

Dashboard może korzystać z device-info i opcjonalnego podglądu menu. Snapshot menu jest obrazem firmware, a nie brakującym mechanizmem przechwytywania obrazu gry. Workflow MRTR może rozdzielać załadowanie i uruchomienie; obie czynności mają rzeczywiste skutki na urządzeniu. Nowe narzędzia dopisać do allowlisty dodatków w testach katalogów, zachowując bazowe kontrakty starych narzędzi.

## Risks / Trade-offs

- Dokumentacja latest może wyprzedzać zainstalowany firmware → fake kontrakty i jasny NotSupported; testy na urządzeniu to osobny etap.
- Menu używa kodów firmware, nie zwykłego tekstu → surowe macierze są źródłem prawdy, renderer jest pomocniczy.
- Pomylenie load z run/reset → test HTTP metody i endpointu, precyzyjne opisy skutków.
- Nakładanie się planów → wspólny transport ma jednego właściciela i jest zależnością planu MCP.

## Migration Plan

Wdrażać w kolejności priorytetów z tasks.md. Po każdym etapie uruchomić właściwe testy i zachować 38 obecnych przypadków. Nowe domyślne smoke ograniczyć do odczytów; operacje zapisu mają osobne scenariusze z fake urządzeniem. Rollback przywraca kod/konfigurację klienta; nie może cofnąć wcześniej wykonanej operacji sprzętowej.

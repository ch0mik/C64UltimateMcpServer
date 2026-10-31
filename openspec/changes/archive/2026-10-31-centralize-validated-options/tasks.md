# Tasks

Wdrożono 2026-10-31. Dowody: verification.md, review.md, completion-review.md oraz testy konfiguracji.

## 1. Kontrakt i fundament

- [x] 1.1 Zweryfikować inventory.md względem wszystkich odczytów IConfiguration/env, stałych operacyjnych, JSON, Docker i frameworkowych Options; każda pozycja ma docelową sekcję lub uzasadnionego właściciela framework/tooling.
- [x] 1.2 Dodać modele z domyślnymi wartościami, sekcjami i zakresami design.md, zachowując Ultimate i Mcp:OAuth; ustalić jeden właściciel defaultów i test parytetu appsettings.
- [x] 1.3 Dodać AddValidatedApplicationOptions, strict binding, IValidateOptions oraz ValidateOnStart dla wszystkich własnych sekcji i bezcykliczną walidację między sekcjami; błędy sekretów nie zawierają wartości.
- [x] 1.4 Testy bindingu pokrywają brak sekcji, env/CLI precedence, błędne typy, nieznane klucze i podsekcje, tablice, minima/maksima, overflow i bezpieczny błąd startu również dla leniwego konsumenta.

## 2. Połączenia

- [x] 2.1 Zastąpić bezpośredni IConfiguration w aktywnych ścieżkach Program, UltimateService i UltimateDeviceClientFactory przez zwalidowane IOptions<UltimateClientOptions>, zachowując mapowanie Ultimate__BaseUrl -> Ultimate:BaseUrl -> Value.BaseUrl; zachować klienta bez DI, stare konstruktory/sygnatury i jawny timeout argument.
- [x] 2.2 Przenieść dwa downloady SID/PRG do wspólnego klienta/fabryki z DownloadOptions.RequestTimeout; potwierdzić disposal, cancellation i brak X-Password na zewnętrznym originie. Nie dodawać w tym refaktorze nowych limitów download size ani polityk URL.
- [x] 2.3 Test HTTP fake handlera potwierdza niestandardowy adres/timeout/password, rzeczywiste mapowanie env Ultimate__BaseUrl z izolacją/przywróceniem środowiska, odmowę startu dla błędnego URL oraz snapshot po zmianie konfiguracji; logi i błędy nie ujawniają sekretu, przekierowania urządzenia pozostają wyłączone.

## 3. Pamięć, workflow i UI

- [x] 3.1 Migrować artefakty i Tasks do IOptions, w tym TTL, budżety, poll interval i retencję; test zegara i niestandardowych limitów potwierdza faktyczne zachowanie.
- [x] 3.2 Migrować ledger, rundy, source/prompt limity, sampling i roots; sprawdzić limit we wszystkich ścieżkach input/sampling i min(deadline operacji, artifact expiry), bez replay ani drugiego efektu.
- [x] 3.3 Migrować live polling i kolejki; fake time sprawdza niestandardowy interwał, coalescing, odłączenie i minimum pojemności.
- [x] 3.4 Migrować TTL dokumentacji i PreviewBytes; test raw HTTP sprawdza nową wartość oraz niezmienione cache scope, TTL artefaktów i legacy wire contract. UI/tekst nadal pokazują ten sam ograniczony podgląd.
- [x] 3.5 Migrować legacy logowanie i cleanup do opcji, z opcjonalną flagą oraz walidacją timeoutu hosta; testy izolacji sesji i zatrzymania workers oraz wyłączenia samego logging sender bez wyłączania subskrypcji.

## 4. Host i zgodność

- [x] 4.1 Migrować identity (w tym ikonki) i transport do typowanych opcji; sprawdzić nazwy enumów, routing i kolizje endpointów oraz defaulty katalogów/handshake. Testy obejmują wszystkie trzy SessionMode (oczekiwaną odmowę modern w Stateful i brak legacy sesji w Stateless), niestandardowe EndpointPath/HealthPath oraz OAuth na wszystkich wygenerowanych trasach.
- [x] 4.2 Ujednolicić OAuth registration i ClockSkew; zachować default false, walidację warunkową, 401/403 i publiczne metadata za proxy przy konfigurowanych ścieżkach.
- [x] 4.3 Udokumentować i sprawdzić standardowe framework Options dla Host, Kestrel, Logging, AllowedHosts i urls/environment; bez duplikowania sekcji i bez wyłączania istniejącego reload log level.
- [x] 4.4 Zaktualizować JSON/env/CLI/Docker i kompletny przykład, potwierdzając brak sekretów, zgodność kluczy i array merging; nie wykonywać deploymentu.

## 5. Weryfikacja i zamknięcie

- [x] 5.1 Uruchomić pełne Release tests (obecna baza 99), 10 offline HTTP example files oraz właściwe testy UI/browser dla zmienionego preview/identity/routing; bazowe schematy narzędzi zachowane.
- [x] 5.2 Wykazać audytem brak aplikacyjnych odczytów IConfiguration/env poza composition/adapters i brak niezmapowanych stałych operacyjnych; zapisać coverage i wyniki w verification.md.
- [x] 5.3 Wykonać niezależny review subagentem, poprawić istotne uwagi, uruchomić OpenSpec strict i diff checks.
- [x] 5.4 Uaktualnić README, przewodniki, specyfikacje i angielskie RELEASE_NOTES.md wyłącznie o wdrożone zachowanie, dostarczyć raw MD; zarchiwizować po ukończeniu i review.

## Final audit follow-up

Task 1.4 został ponownie sprawdzony: domknięto testy wszystkich granic, overflow i rzeczywistego ClockSkew JWT. Końcowy niezależny review potwierdził brak nowych istotnych problemów. Finalny wynik 236/236; pozostałe zadania osobnego planu REST nie zostały oznaczone jako wykonane.

# Design

## Context

Plan z 2026-10-30, przygotowany po prośbie o pełny zakres nowych funkcji, włącznie z MCP Apps i MRTR. Migracja 1.4.0 → 2.3.0 jest wdrożona: 38 testów Release oraz osiem przykładów HTTP przechodzi. Funkcje wdrożono 2026-10-31; końcowe wyniki opisuje verification.md.

Punktem odniesienia jest tag SDK `v2.3.0` (`9a327829ef066c44a0d737c58bf3c7617d7c701c`), nie lokalny HEAD będący 16 commitów dalej. Źródła: [wydania](https://github.com/modelcontextprotocol/csharp-sdk/releases), dokumentacja [Apps](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/apps/apps.md), [MRTR](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/mrtr/mrtr.md), [Tasks](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/tasks/tasks.md) i [zasobów](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/docs/concepts/resources/resources.md). Kontrakty cache potwierdza `ICacheableResult` w tagu.

SDK 2.0 wnosi MRTR, cache, Apps i nowe Tasks, 2.1 publiczny handler subscriptions/listen, 2.2 transport hybrydowy, a 2.3 poprawki discovery, kolejności katalogów i zachowania HTTP. Zatem pełny zakres dotyczy możliwości dostępnych w 2.3.0, nie wyłącznie funkcji dodanych w samym wydaniu 2.3.

Rozszerzenia Skills widoczne w lokalnym HEAD nie istnieją w drzewie tagu 2.3.0. Nie planujemy ich jako funkcji tego NuGet. OAuth, stdio i klientowy TimeProvider nie tworzą funkcji użytkowych dla tego serwera HTTP.

## Goals / Non-Goals

**Goals:** obsługa nowoczesnego klienta bez sesji, wygodna praca z programami, interfejs Apps oraz testowalne interakcje i operacje w tle. Nowe narzędzia są dodatkiem do istniejącego API.

**Non-Goals:** emulator C64, pełne IDE, zrzuty ekranu bez istniejącego źródła obrazu, backend wieloprocesowy, trwała baza zadań i plików, projektowanie uwierzytelniania, hosting zewnętrznej witryny lub aktualizacja powyżej 2.3.0.

## Decisions

### Zależność od przeglądu API urządzenia

Późniejsza analiza dokumentacji REST jest zapisana w `../../align-ultimate-rest-api/`. Ten plan wykorzystuje z niej wspólny transport klienta, NetworkPassword, klasyfikację błędów i CancellationToken zamiast wdrażać drugi wariant. Device-info może zasilać dashboard; opcjonalny odczyt menu dotyczy wyłącznie firmware Ultimate i nie stanowi zrzutu ekranu gry. Brak funkcji w zainstalowanym firmware ma czytelny wynik NotSupported. Wdrożenie funkcji zależnych od nowego klienta następuje po odpowiednich zadaniach planu API.

### 1. Moduły i kolejność

Rozdzielić klienta urządzenia, magazyn artefaktów, obserwację stanu, politykę cache, orchestrator workflow i zasoby UI. `Program.cs` rejestruje moduły. Kolejność realizacji: artefakty i anulowanie → Tasks → obserwacja/cache → MRTR → Apps. Każdy etap dostarcza testy i dokumentację.

Zachować jeden proces i pamięciowy stan aplikacyjny. Bezsesyjność transportu nie oznacza braku magazynów artefaktów, operacji czy zadań. Po restarcie identyfikatory tracą ważność; klient otrzymuje jawny błąd i rozpoczyna nowy workflow.

### 2. Rzeczywiste zasoby i subscriptions/listen

`c64://device/status` zawiera dostępność, wersję API i czas obserwacji; nie raportuje zmyślonego stanu CPU. `c64://device/drives` udostępnia dane istniejącego API napędów. Format to tekstowy zasób z MIME `application/json` i stabilnym schematem.

Wspólny obserwator odpytuje urządzenie domyślnie co 5 sekund wyłącznie przy aktywnych subskrypcjach; jednorazowy odczyt odświeża snapshot. Porównuje znormalizowane dane bez pola czasu obserwacji. Zmiana dostępności jest zmianą treści. Nie rozsyła fałszywych aktualizacji dokumentacji, której treść się nie zmieniła.

Handler `WithSubscriptionsListenHandler` wysyła najpierw dokładnie jedno acknowledged, następnie wyłącznie uzgodnione zdarzenia z `_meta.subscriptionId`. Capabilities ustawia jawnie na rzeczywiście obsługiwane typy. Stan listenera jest związany z żądaniem strumienia, nie z `Mcp-Session-Id`. Kolejka listenera ma limit 32 wpisów; kolejne aktualizacje tej samej URI można scalić. Nieograniczona kolejka lub odpytywanie po rozłączeniu byłyby niepotrzebnym kosztem.

Starsze resources/subscribe i unsubscribe nadal działają; dla nowych zasobów korzystają z tego samego obserwatora. Istniejący okresowy nadawca legacy zostanie ograniczony do faktycznych zmian zamiast ogłaszania zmian niezmienionej dokumentacji. Istniejący kontrakt powiadomień zachowuje nazwy i URI; zmienia się częstotliwość na zależną od treści.

### 3. Polityka cache

Ustawić `TimeToLive` i `CacheScope` przez filtry wyników `ICacheableResult`, bez przebudowy wszystkich metod zasobów. SDK serializuje TTL do `ttlMs` i ogranicza nowe pola do właściwych wersji protokołu.

| Odpowiedź | TTL | Scope |
| --- | --- | --- |
| Wbudowana dokumentacja c64:// | 1 godzina | public |
| Wszystkie katalogi | 0 | private |
| Discovery oraz katalogi zależne od Apps/capabilities | 0 | private |
| Snapshoty urządzenia | 0 | private |
| Artefakty i ich katalog | najwyżej pozostała ważność; katalog 0 | private |

Publiczny cache wybrać tylko dla identycznych danych. Nie ustawiać public na katalogu z narzędziami app-only albo zasobami artefaktów. Zdarzenie aktualizacji unieważnia cache mimo niewygasłego TTL. Domyślnie nie dodawać własnego cache wyników narzędzi mających skutki uboczne.

### 4. Artefakty i wyniki binarne

`ProgramArtifactStore` przechowuje niezmienne bajty i metadane, nadając losowy identyfikator bez ścieżki plikowej. Parametry domyślne: TTL 60 minut, 4 MiB na plik, 16 MiB i 256 wpisów łącznie. Przy zapisie usuwa przeterminowane, potem najstarsze wpisy; istniejące ID nie są ponownie używane.

`resources/read` zwraca PRG jako binarną zawartość zasobu z MIME `application/octet-stream`; metadane są dostępne w wyniku workflow/preview. SHA-256 liczyć z tych samych bajtów, które serwuje zasób. Rzut hex ograniczyć do pierwszych 256 bajtów. Nie tworzyć plików na ścieżkach dostarczonych przez klienta. Nowy workflow zwraca URI i metadane, stare generatory zachowują base64.

Magazyn jest przeznaczony do tej samej zaufanej, pojedynczej instancji co obecny serwer. `private` w cache nie zastępuje autoryzacji i nie daje izolacji użytkowników; plan nie deklaruje takiej izolacji.

### 5. Tasks, postęp i anulowanie

Zachować `WithTasks` i opt-in. Zastąpić nieograniczony store implementacją `IMcpTaskStore` z adapterem retencji: limit 256 rekordów, terminalne wyniki przez 30 minut, aktywne rekordy nie są usuwane w celu zwolnienia miejsca. Polling i cancellation mają zachować standardowe formaty SDK.

Wynik polling otrzymuje `statusMessage` określający fazę: zbieranie danych, kompilacja, oczekiwanie na potwierdzenie, wysyłanie, wykonanie lub zakończenie. Dla zwykłych długich żądań emitować progress tylko przy przekazanym tokenie, zachowując jego typ string/liczba. Po zwróceniu CreateTaskResult modern HTTP nie polegać na powiadomieniach związanych z zakończonym żądaniem; stan przekazywać przez polling.

Dodać opcjonalne tokeny do operacji klienta REST oraz metod narzędzi i przekazywać je do HTTP oraz odczytu odpowiedzi. Nie serializować tokenu jako parametru narzędzia. Wykorzystać kontrolowaną fabrykę klienta/HttpMessageHandler z planu `align-ultimate-rest-api`, bez zerwania istniejącego konstruktora i z wyłączonymi przekierowaniami także dla DI. Dzięki temu testy mogą zatrzymać HTTP w kontrolowanym handlerze. Wyjątek anulowania nie może być zamieniany na sukces ani zwykły błąd urządzenia.

### 6. MRTR i workflow programu

Nowe narzędzie `ultimate_program_workflow` przyjmuje język (`basic`/`assembly`), opcjonalne źródło lub opis oczekiwanego programu i zamiar wykonania. Kolejne fazy:

1. Elicitation brakujących danych, jeśli klient deklaruje obsługę. Brak możliwości interakcji daje komplet wymaganych argumentów do ponownego zwykłego wywołania.
2. Opcjonalne sampling po źródło, tylko na żądanie generowania i z capability klienta. Źródło przechodzi lokalny generator i walidację.
3. Opcjonalne roots przy wyborze projektu po stronie klienta; ścieżka klienta jest kontekstem, a kod musi przyjść jako treść. Serwer nie otwiera takiej ścieżki lokalnie.
4. Kompilacja i zapis artefaktu. Domyślnie workflow kończy się tutaj.
5. Jeśli wykonanie było zamówione, pokazać artefakt, jego SHA-256 i adres urządzenia w żądaniu potwierdzenia; dopiero akceptacja pozwala wywołać istniejący klient REST.

Bezsesyjna ścieżka używa `InputRequiredException`/`InputRequiredResult`, `InputResponses` i `RequestState`; nie wywołuje legacy server-to-client ElicitAsync/SampleAsync/RequestRootsAsync. Starsza ścieżka korzysta z resolvera zgodności SDK przy odpowiednich capability. SDK 2.3.0 nie komponuje MRTR z Tasks w narzędziach atrybutowych. Dedykowany WorkflowTaskRunner przechwytuje workflow przed filtrem SDK, udostępnia input_required i obsługuje tasks/update. Testy muszą objąć obie formy.

`RequestState` jest nieprzewidywalnym identyfikatorem rekordu operacji, nie zaufanym opisem operacji od klienta. Rekord zawiera hash parametrów, target, artifact ID, fazę, ważność i wersję rundy. Limit 256, ważność maksymalnie 60 minut i nie dłuższa niż artefaktu. Atomowe przejście confirmed → executing zapobiega podwójnemu wykonaniu po ponowieniu żądania. Duplikat otrzymuje błąd ponowienia; niepewny wynik po zerwaniu komunikacji nie powoduje automatycznego powtórzenia urządzenia. Zmiana danych, celu lub wygasły rekord wymaga nowej operacji.

### 7. MCP Apps

Dodać `ModelContextProtocol.Extensions.Apps` 2.3.0 i `WithMcpApps`, lokalnie wyciszając MCPEXP003 w module UI z komentarzem. API Apps jest eksperymentalne także w tym stabilnym wydaniu SDK. Nie polegać na zmianach serializacji dodanych po tagu.

Nowe narzędzia `ultimate_dashboard` i `ultimate_program_preview` deklarują UI przez `McpAppUi`/`_meta.ui`, odpowiednio `ui://c64/dashboard.html` i `ui://c64/program-preview.html`. Zasoby mają MIME `text/html;profile=mcp-app`. HTML i skrypt mostu hosta są dostarczane razem z aplikacją, bez CDN. Ewentualna zależność mostu JS ma przypiętą wersję i lockfile.

Dashboard pokazuje availability, wersję API i napędy; odświeżanie używa MCP host bridge. Preview przyjmuje ID artefaktu i pokazuje metadane, diagnostykę i ograniczony hex. UI nie łączy się bezpośrednio z prywatnym adresem C64. Wyjście tekstowe i structuredContent są pełnowartościowe bez Apps. Przyciski prowadzące do wykonania korzystają z potwierdzanego workflow, nie z automatycznego wykonania przy renderowaniu strony.

Polityka zasobu blokuje niepotrzebne zewnętrzne originy; wszystkie źródła i komunikaty wyświetlać jako tekst. Narzędzia app-only mają osobne przeznaczenie i nie są pokazywane modelowi klienta, który nie negocjuje UI. Nie przyjmować jako faktu, że każdy host obsługuje fullscreen/pip — tryb prezentacji negocjuje host.

## Risks / Trade-offs

- Apps jest eksperymentalne → wydzielony moduł, przypięte 2.3.0 i test alternatywy tekstowej.
- MRTR może ponowić handler → ledger faz i atomowe zużycie potwierdzenia przed wykonaniem efektu.
- Odłączenie/timeout nie daje pewności niewykonania komendy → raport niepewnego wyniku bez automatycznego retry skutku ubocznego.
- Testy SDK mogą ukryć fallback protokołu → testować surowe HTTP i pinować wersje, jak w migracji.
- Zasoby urządzenia zależą od sprzętu → fake HttpMessageHandler w automatyzacji; osobna opcjonalna walidacja na C64 po uzgodnieniu.
- Pamięciowe ID nie działają po restarcie ani w niezależnej instancji → jawne wygasanie i dokumentowany model jednej instancji.

## Migration Plan

Realizować etapy z tasks.md, utrzymując przechodzące istniejące 38 testów i osiem przykładów. Każdy moduł rejestrować dopiero po dodaniu wymaganych testów i opisów. Nowe URI/narzędzia porównać z bazowymi katalogami jako zaplanowane dodatki, a nie regresję.

Po zakończeniu przeprowadzić testy Apps w rzeczywistym kompatybilnym hoście i zapisać jego wersję; test HTML bez hosta nie dowodzi zgodności mostu. Wdrożenie jest oddzielnym krokiem. Rollback przywraca poprzedni obraz/kod; nowe zadania, artefakty i operacje w pamięci przepadają, a klienci zaczynają nowe interakcje.

## Implementation decisions (2026-10-31)

Dashboard używa istniejących GetVersion/GetDrives, bez planowanego device-info. Wszystkie katalogi mają private/0. App-only helper ultimate_request_program_execution opakowuje prośbę MRTR jako zwykły CallToolResult, wymagany przez most Apps, i przekazuje kontynuację do rozmowy hosta. Sam helper nie wykonuje komendy. Źródło ograniczono do 1 MiB, prompt do 64 KiB i osiem rund. Host referencyjny używa AppBridge 2.0.4 w Edge 156.0.4314.50; nie deklarujemy testów komercyjnych hostów.

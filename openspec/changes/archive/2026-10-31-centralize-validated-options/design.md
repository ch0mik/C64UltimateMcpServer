# Design

## Status

Wdrożono 2026-10-31 po osobnym poleceniu użytkownika, na podstawie inwentaryzacji istniejących konsumentów i dokumentacji Options: https://learn.microsoft.com/en-us/dotnet/core/extensions/options . Nie zastępuje raportów wcześniejszych, wdrożonych zmian.

## Rejestracja i konsumpcja

Dodać AddValidatedApplicationOptions(configuration) w Configuration. Każdy własny model binduje dokładną sekcję ze strict binding i ValidateOnStart; zachować walidację OAuth. IValidateOptions obsługuje reguły złożone, walidator złożony zależności między sekcjami. Nie budować pomocniczego ServiceProvider. Unikać cyklu walidatorów: walidator złożony zależy od zwalidowanych opcji bazowych, bez zależności odwrotnej. Zarejestrować walidację także sekcji konsumowanych leniwie, aby błędy wystąpiły przed uruchomieniem listenerów i workers.

Własne usługi konsumują IOptions<T>.Value i przechowują niemodyfikowaną kopię/snapshot; nie stosować IOptionsSnapshot w singletonach. Fabryki tworzą kopie opcji klienta. Zmiana konfiguracji aplikacji wymaga restartu i nie zmienia targetu w środku potwierdzanego workflow. Zachować natywną semantykę opcji frameworka, w tym reload poziomów logowania; nie udawać, że IOptions zapewnia hot reload. Publiczne legacy konstruktory zachować delegując do jednego adaptera typowanych ustawień, bez osobnej ścieżki pomijającej walidację.

## Walidacja

- Ultimate.BaseUrl: absolutny HTTP/HTTPS origin (root path), bez userinfo/query/fragment; zachować HTTP dla lokalnego sprzętu. NetworkPassword opcjonalny, bez CR/LF i niedozwolonych znaków nagłówka; nigdy nie wypisywać wartości. Błędny URL nie może uruchomić klienta HTTP.
- RequestTimeout (Ultimate, Downloads): 100 ms..10 min, skończony; bez InfiniteTimeSpan. Zachować osobne klienty urządzenia i downloadu, brak X-Password na zewnętrznych żądaniach i blokadę przekierowań urządzenia.
- Pojemności magazynów/ledger: 1..10000; artefakt co najmniej 2 bajty, maksymalnie 64 MiB, całkowity budżet do 512 MiB i MaxArtifactBytes <= MaxBytes. Arytmetyka wielkości na long/checked, brak overflow przy sumie.
- Lifetime artefaktu i kontynuacji: 1 s..24 h; retencja terminalnych Tasks: 1 s..7 dni. ContinuationLifetime <= Artifact.Lifetime, a rzeczywisty deadline operacji to min(deadline operacji, expiry artefaktu), także po kompilacji/reuse. Nie przedłużać kontynuacji do TTL artefaktu.
- Task PollInterval: 10 ms..1 min i mniejszy od TerminalRetention. Polling live: 100 ms..1 h, bez warunku timeout <= interwał (odczyty są serializowane).
- ListenerQueueCapacity: 2..4096; minimum równe liczbie obecnie wspieranych live URI, aby DropOldest nie gubił URI pozostającej w zbiorze coalescing. Zmianę algorytmu/drop policy traktować oddzielnie od konfiguracji.
- Workflow: MaxRounds 1..64, MaxSourceBytes 1..16777216, MaxGenerationRequestBytes 1..1048576, SamplingMaxTokens 1..32768, MaxRootHints 1..1000. Stosować ten sam limit także do odpowiedzi elicitation i sampling oraz diagnostyki, nie tylko pierwszych argumentów.
- Cache dokumentacji: TTL od 0 do 24 h; prywatność/live TTL=0 oraz artifact TTL <= remaining lifetime pozostają stałe. PreviewBytes: 1..256, bez zwiększenia obecnego maksymalnego podglądu.
- LegacyLoggingInterval i CleanupTimeout: 100 ms..5 min; interwał logowania wymagany tylko gdy flaga enabled, typ nadal poprawny. CleanupTimeout <= HostOptions.ShutdownTimeout; sprawdzić dodatni, skończony timeout hosta w docelowym runtime.
- SessionMode: tylko nazwane wartości SDK 2.3.0 Stateless, Stateful i StatefulForInitializeClients, bez niezdefiniowanych liczbowych enumów. IdleTimeout 1 s..7 dni, MaxIdleSessionCount 1..100000; są to limity idle, nie aktywnych sesji. Ustawienia idle mają zastosowanie wyłącznie do trybów utrzymujących sesje; SDK MCP9006 tłumić lokalnie z komentarzem. Nie włączać przestarzałego /sse przez tę migrację.
- EndpointPath i HealthPath: jednoznaczne lokalne ścieżki zaczynające się /, bez query/fragment, wildcardów, parametrów tras, segmentów . lub .. i zakodowanych separatorów; różne od siebie, bez kolizji endpointów MCP/health/metadata (w tym generowanych podtras SDK). Zarezerwować prefix /.well-known/oauth-protected-resource. Zachować domyślne / i /health.
- OAuth: zachować obecne reguły warunkowe, dodać ClockSkew 0..5 min, domyślnie 30 s. Publiczny Audience może zawierać ścieżkę proxy; nie wymagać prostego równania Audience.Path == EndpointPath. Zweryfikować proxy rewrite i SDK metadata path w testach.
- Identity: Name/Version niepuste, opcjonalne Title/Description; URL i ikonki poprawne, HTTPS dla zewnętrznych URL ikon/website, sensowne MIME image/*, Theme light/dark lub brak, poprawne Sizes. Tablice startują puste; obecne ikony pochodzą z jednej konfiguracji domyślnej, bez doklejania podczas bindingu.

Reguły zakresów wdrożono w ApplicationOptionsRegistration i validatorze OAuth. Pokryć minima, maksima i wartości tuż poza nimi. Błędy identyfikują sekcję/klucz; nie logować całych obiektów opcji ani raw exception z sekretami. Binding błędnego typu i nieznanych kluczy także musi bezpiecznie zatrzymywać start. Walidacja bez dostępu do urządzenia, IdP lub sieci.

## Zgodność i konfiguracja frameworka

Zachować precedencję JSON -> JSON środowiska -> secrets/env -> CLI używaną przez host. Sekcje nieobecne otrzymują obecne wartości domyślne. Dla dodawanych pól nie kopiować defaultów do kilku niezależnych miejsc; test parytetu default object i appsettings wychwytuje drift. Strict binding tylko we własnych sekcjach (walidacja Mcp namespace uwzględnia wszystkie znane podsekcje), nie na całym root, aby nie odrzucać kluczy hosta i providerów.

Frameworkowe Kestrel/Logging/HostOptions/HostFilteringOptions i konfiguracja urls pozostają natywne. Zinwentaryzować faktycznie udostępnione sekcje, użyć ich typowanych opcji tam, gdzie kod aplikacji ich potrzebuje. Nie tworzyć własnego drugiego Kestrel ani ręcznie zastępować parsera ASPNETCORE_URLS. Dodać startup walidację spójności ścieżek i timeoutów aplikacji z odpowiednimi opcjami frameworka. Porty i Docker healthcheck pozostają po stronie orkiestratora.

Nie zmieniać starych schematów parametrów/wyników narzędzi ani semantyki ultimate_set_connection (obecnie nie przełącza targetu). Testy wstrzykują skonfigurowane IOptions/Configure/PostConfigure, zamiast produkcyjnych init override limitów. Zachować testowalny TimeProvider i HttpMessageHandler.

## Realizacja

1. Zatwierdzić inwentaryzację i modele; przygotować testy bindingu/validatorów i composition root.
2. Migrować Ultimate, fabrykę i downloady, zachowując ABI i izolację sekretów.
3. Migrować magazyny i workflow, następnie obserwację, cache, preview i workers.
4. Migrować identity/transport, ujednolicić OAuth; sprawdzić host/framework i przykłady wdrożeniowe.
5. Zweryfikować niestandardowe wartości w prawdziwym HTTP z atrapą urządzenia, pełne regresje, subagent review i dokumentację.

Nie wymaga wdrożenia ani fizycznego C64. Po kodzie, testach i review specyfikacje główne są synchronizowane przez archiwizację.

## Review planu

Niezależny subagent wskazał konieczność zmiany mcp-http-compatibility. Dodano delta spec: domyślna konfiguracja zachowuje oba protokoły i obecne ścieżki; jawny Stateful odmawia modern discovery zgodnie z SDK, Stateless nie utrzymuje legacy sesji, a wyłączenie logowania nie zatrzymuje subskrypcji. Testy muszą pokryć trzy tryby, zmienione ścieżki i zachowanie OAuth na wszystkich wygenerowanych trasach.

## Implementation details

ConfigurationBinder potrafi ignorować scalar zamiast obiektu/tablicy; ValidateShape weryfikuje strukturę rekursywnie przed safe strict bind. Błędy typów nie przechowują oryginalnego wyjątku z wartościami. LegacyConfigurationAdapter używa wspólnych validatorów Ultimate i Downloads.

ValidateOnStart waliduje cache IOptionsMonitor, która jest oddzielna od IOptions.Value. Pierwszy hosted initializer wymusza snapshot wszystkich własnych IOptions w konstruktorze, przed skonstruowaniem workers, również dla leniwych modułów. Native logging zachowuje własny reload.

SDK WithTasks w 2.3.0 wymaga gotowej instancji magazynu przed budową DI; SdkTaskStoreAdapter otrzymuje DI-owned BoundedTaskStore po Build i przed MapMcp. Nie budujemy pomocniczego ServiceProvider. Tryb i metadane SDK konfiguruje OptionsBuilder z zależnością od typowanych opcji aplikacji.

Publiczne konstruktory serwera z IConfiguration pozostają adapterami zgodności; aktywacja SDK oznacza preferowany konstruktor typed/factory przez ActivatorUtilitiesConstructor. Samodzielny klient zachowuje ABI i pierwszeństwo jawnego argumentu timeout.

# Inwentaryzacja konfiguracji — 2026-10-31

Sekcje i klasy wdrożono 2026-10-31; weryfikację opisuje verification.md. Wartości domyślne zachowują aktualne zachowanie, jeśli niżej nie opisano jawnie nowej opcji.

| Sekcja / Options | Ustawienia i domyślne wartości | Konsument |
| --- | --- | --- |
| Ultimate / UltimateClientOptions | BaseUrl=http://192.168.0.120, NetworkPassword=null, RequestTimeout=30 s (nowe pole) | Program.cs, UltimateService, UltimateDeviceClientFactory; domyślny timeout klienta |
| Downloads / DownloadOptions | RequestTimeout=100 s (obecny domyślny HttpClient); wyłącznie timeout pobierania SID/PRG z URL | Dwa downloady UltimateService przez DownloadClientFactory |
| Mcp:Artifacts / ProgramArtifactOptions | MaxEntries=256, MaxBytes=16777216, MaxArtifactBytes=4194304, Lifetime=1 h | ProgramArtifactStore |
| Mcp:Tasks / McpTaskStoreOptions | Capacity=256, TerminalRetention=30 min, PollInterval=250 ms | BoundedTaskStore, rejestracja WithTasks |
| Mcp:Workflow / ProgramWorkflowOptions | Capacity=256, ContinuationLifetime=1 h, MaxRounds=8, MaxSourceBytes=1048576, MaxGenerationRequestBytes=65536, SamplingMaxTokens=4096, MaxRootHints=20 | ProgramWorkflow (także odpowiedzi elicitation/sampling) |
| Mcp:LiveResources / LiveResourceOptions | PollInterval=5 s, ListenerQueueCapacity=32 | DeviceResourceHub |
| Mcp:Cache / ContentCacheOptions | DocumentationTimeToLive=1 h | FeatureProtocol |
| Mcp:Apps / McpAppsOptions | PreviewBytes=256, konfigurowalne w dół w zakresie 1..256 | AppTools; opisy narzędzi pozostają prawdziwe |
| Mcp:Notifications / McpNotificationOptions | LegacyLoggingEnabled=true (nowa flaga zachowująca obecne działanie), LegacyLoggingInterval=15 s, CleanupTimeout=5 s | LoggingUpdateMessageSender, McpSessionNotifications |
| Mcp:Transport / McpTransportOptions | EndpointPath=/, HealthPath=/health, SessionMode=StatefulForInitializeClients, IdleTimeout=2 h, MaxIdleSessionCount=10000 | MapMcp, health mapping i HttpServerTransportOptions (dwie ostatnie wartości obecnie pochodzą z SDK 2.3.0) |
| Mcp:Server / McpServerIdentityOptions | Name, Version, Title, Description, WebsiteUrl i Icons z obecnego Program.cs | ServerInfo; ikony zawierają Source, MimeType, Sizes, Theme |
| Mcp:OAuth / McpOAuthOptions | Zachować Enabled=false, Authority, Audience, RequiredScopes; wydzielić ClockSkew=30 s do opcji | Istniejący validator i McpOAuthConfiguration |
| Host / framework HostOptions | ShutdownTimeout zgodny z faktycznym domyślnym HostOptions użytej wersji; jawna dokumentacja | Framework host i zatrzymanie workers |
| Logging, AllowedHosts, Kestrel, urls / ASPNETCORE_URLS, environment | Standardowe opcje frameworka; zachować obecne klucze, precedencję i celowy reload log level | WebApplicationBuilder / ASP.NET Core |

## Jawne mapowanie Ultimate__BaseUrl

Zachować istniejącą zmienną środowiskową `Ultimate__BaseUrl` (także Docker). Provider .NET mapuje ją na `Ultimate:BaseUrl`, bind sekcji `Ultimate` wypełnia `UltimateClientOptions.BaseUrl`, a konsumenci odczytują ją przez `IOptions<UltimateClientOptions>.Value.BaseUrl`. To ten sam kontrakt dla JSON, env i CLI, bez osobnego odczytu env w usługach. `Ultimate__NetworkPassword` i `Ultimate__RequestTimeout` korzystają z tego samego modelu.

Validator opcji sprawdza BaseUrl przed startem (HTTP/HTTPS origin bez userinfo/query/fragment), niezależnie od źródła wartości. Test musi ustawić rzeczywiste env `Ultimate__BaseUrl` przy kontrolowanej izolacji testu, potwierdzić nadpisanie JSON i wywołanie właściwego fake endpointu; nieprawidłowa wartość ma zatrzymać host przed HTTP do urządzenia. Zmienną przywrócić po teście, bez konfliktu z równoległymi testami.

## Zakres migracji

Własne ustawienia aplikacji muszą mieć jednego właściciela, źródło domyślne i validator. IConfiguration oraz odczyty env są dozwolone w composition root i adapterach konfiguracji frameworka, nie w fabrykach urządzenia i logice narzędzi. Nie tworzyć drugiego modelu konfiguracji o tej samej semantyce.

Samodzielny Ultimate64Client nadal przyjmuje obecne konstruktory i zwykły UltimateClientOptions. Opcja RequestTimeout nie usuwa starego argumentu timeout; jawny argument istniejącego konstruktora zachowuje pierwszeństwo, a fabryka serwera przekazuje zwalidowaną wartość. Nie dodawać IOptions do generatorów PRG ani do interfejsów REST użytkownika.

Stałe kontraktów (URI zasobów, nazwy narzędzi, wersje protokołu, MIME, klucze _meta, format PRG, długości losowych ID i algorytm SHA-256) pozostają kodem. Argumenty wywołania narzędzi, np. długość odczytu pamięci, nie są konfiguracją hosta. CacheScope i TTL=0 dla danych prywatnych/live pozostają nieosłabialnymi regułami.

Node/npm/browser test env, wersje zależności, parametry build, mapowania portów i Docker healthcheck to konfiguracja narzędzi/orkiestratora. Pozostają w natywnych formatach, a przekazywane wartości aplikacyjne mapują do tych samych sekcji Options. Kontrole frameworka i ta lista wyjątków muszą zostać zapisane w końcowej macierzy pokrycia, aby słowo „wszystko” nie ukrywało odczytów aplikacyjnych.

## Powiązanie z innymi zmianami

align-ultimate-rest-api nadal planuje walidację argumentów REST i nowe endpointy. Ta zmiana jest właścicielem konfiguracji transportu/poświadczeń/timeoutów, a nie walidacji poleceń urządzenia. Nie implementować drugich opcji w planie REST. OAuth jest już wdrożone; ta zmiana ujednolica rejestrację i dodaje ClockSkew, zachowując ochronę oraz domyślne wyłączenie.

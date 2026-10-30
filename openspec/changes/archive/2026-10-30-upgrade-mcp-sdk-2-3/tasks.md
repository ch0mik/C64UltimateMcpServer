# Tasks

Implementacja zakończona. Wyniki testów i pokrycie scenariuszy opisano w verification.md.

## 1. Punkt odniesienia

- [x] 1.1 Uruchomić `dotnet test C64UltimateMcpServer.slnx` na MCP 1.4.0 i zapisać wynik; oddzielić wcześniejsze niepowodzenia od regresji migracji.
- [x] 1.2 Zebrać katalogi narzędzi, promptów i zasobów oraz odpowiedzi generatorów i narzędzia zwracającego string bez kontaktu ze sprzętem; zachować dane do porównania kontraktów po aktualizacji.

## 2. Pakiety, transport i Tasks

- [x] 2.1 Zastosować wersje z tabeli w design.md, dodać referencję Tasks, przejść na `WithTasks` i `StatefulForInitializeClients`; potwierdzić `dotnet restore C64UltimateMcpServer.slnx` oraz `dotnet build C64UltimateMcpServer.slnx --no-restore`, bez błędów API i downgrade pakietów.
- [x] 2.2 Dodać fixture rzeczywistej aplikacji w istniejącym projekcie testowym oraz zależność testowania ASP.NET Core; testami HTTP potwierdzić health, initialize/session/DELETE, discovery bez sesji i brak legacy SSE. Wymusić oba protokoły bez polegania na fallbacku klienta.
- [x] 2.3 Dodać testy kontraktowe katalogów, promptu, zasobu, BASIC/assembly oraz błędu generatora dla obu protokołów; porównać z punktem odniesienia, uwzględniając różnice pól protokołu i kolejności list.
- [x] 2.4 Dodać testy Tasks: reklama rozszerzenia, brak task bez opt-in, opt-in i polling, wynik błędny narzędzia, anulowanie kontrolowanego długiego zadania testowego; potwierdzić stan pomiędzy odrębnymi żądaniami HTTP.
- [x] 2.5 Opisać w obu wersjach językowych README MCP 2.3.0, tryb hybrydowy, nowe Tasks, wymagany opt-in, utratę stanu po restarcie i migrację legacy SSE na `/`; sprawdzić zgodność opisów z testami 2.2–2.4 i usunąć nieaktualne komentarze o SDK 1.0.0.

## 3. Powiadomienia i sesje

- [x] 3.1 Poprawić metodę powiadomienia zasobu na `notifications/resources/updated`, preferując stałą i typowane parametry SDK; test dwóch sesji i unsubscribe ma wykazać prawidłowe URI oraz izolację odbiorców.
- [x] 3.2 Zapewnić zatrzymanie i oczekiwanie na nadawców w cleanup sesji, także przy częściowym starcie i anulowaniu; testami z kontrolowanymi sygnałami potwierdzić brak pozostawionych workerów i usunięcie stanu subskrypcji.
- [x] 3.3 Ograniczyć legacy logowanie do ścieżki sesyjnej, sprawdzić serializację i próg poziomu, stosować tylko lokalne uzasadnione suppression MCPEXP002/MCP9005; testy potwierdzają filtr poziomu i brak nadawców dla modern HTTP.
- [x] 3.4 Udokumentować w README zakres subskrypcji/logowania dla obu protokołów; porównać opis z wynikami testów oraz capabilities discovery, bez reklamowania niezaimplementowanego `subscriptions/listen`.

## 4. Przykłady klientów

- [x] 4.1 Poprawić wszystkie przykłady `.http` na wersję 2025-11-25 i właściwe nagłówki żądań po initialize; wyszukanie `2025-11-05` ma nie zwracać trafień w przykładach, a pierwsze sześć scenariuszy smoke ma przechodzić bez C64.
- [x] 4.2 Dodać samodzielne przykłady discovery 2026-07-28 oraz Tasks na generatorze BASIC; wykonać je na lokalnym serwerze i potwierdzić PRG w wyniku polling. Zaktualizować instrukcje smoke, wskazując przykłady wymagające urządzenia.

## 5. Weryfikacja całości

- [x] 5.1 Wykonać `dotnet test C64UltimateMcpServer.slnx -c Release` i `dotnet list C64UltimateMcpServer/C64UltimateMcpServer.csproj package --include-transitive`; zapisać wyniki i potwierdzić spójny graf MCP 2.3.0 oraz brak nowych niewyjaśnionych ostrzeżeń migracyjnych.
- [x] 5.2 Porównać implementację z każdym scenariuszem obu specyfikacji, wykonać `openspec validate upgrade-mcp-sdk-2-3 --strict` i zapisać raport weryfikacji z ograniczeniami; sprawdzić końcowy diff pod kątem niezamierzonych zmian API C64.

## Workflow follow-up

- Po zakończeniu implementacji i przeglądu zarchiwizować zmianę OpenSPEC oraz zsynchronizować specyfikacje główne; nie archiwizować samego planu jako wykonanej aktualizacji.
- Ewentualne wdrożenie i rollback przeprowadzić według design.md jako oddzielny etap.

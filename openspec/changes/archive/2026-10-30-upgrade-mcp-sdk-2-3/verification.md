# Weryfikacja migracji MCP SDK 2.3.0

Data: 2026-10-30. Zakres obejmuje implementację uprzednio zatwierdzonego planu; dodatkowe funkcje SDK będą osobną zmianą OpenSPEC.

## Wyniki

- Bazowe `dotnet test C64UltimateMcpServer.slnx`: 24/24 testy na SDK 1.4.0. Początkowy problem testhosta ustąpił po odświeżeniu restore i czystej kompilacji; nie zmieniano wersji frameworka testowego.
- Bazowe kontrakty zapisano w `C64UltimateClient.Tests/mcp-1.4-baseline.json` z rzeczywistego serwera 1.4.0: katalogi, prompt, zasób, generatory BASIC/assembly, błąd składni i odpowiedź tekstowa. Żadne żądanie nie kontaktowało się z urządzeniem.
- `dotnet test C64UltimateMcpServer.slnx -c Release`: **38/38**, w tym 14 nowych przypadków migracji. Build i restore przechodzą.
- `scripts/Test-McpExamples.ps1` na rzeczywistym lokalnym serwerze Release: **8/8 plików HTTP** — pierwsze sześć smoke i oba modern. Tasks zakończyło się wynikiem zawierającym PRG.
- Graf pakietów: ModelContextProtocol, AspNetCore, Core i Extensions.Tasks **2.3.0**, AI.Abstractions **10.11.0**; bez błędów downgrade. Zależności testowania HTTP 10.0.13 i kontrolowanego czasu 10.1.0 dodano tylko do projektu testowego.
- Zachowano istniejące ostrzeżenia CS1591 w bibliotekach klienta i generatora assembly. Brak nowych ostrzeżeń migracyjnych. Legacy Logging wycisza lokalnie MCP9005, a callback sesji MCPEXP002.

## Pokrycie specyfikacji

| Scenariusze | Dowód |
| --- | --- |
| Initialize, unikalne sesje, DELETE i health | `McpHttpIntegrationTests` — test transportu i oba warianty testu kontraktów |
| Discovery i wywołania bez sesji, brak workerów modern | Testy rzeczywistego HTTP wymuszają 2026-07-28 i brak Mcp-Session-Id |
| Katalogi, prompt, zasób, generatory i format wyniku | Porównanie z zapisanym kontraktem 1.4.0 dla obu protokołów; inputSchema oraz nazwy/URI zachowane |
| Subskrypcja, dwie sesje, unsubscribe i URI powiadomienia | Test dwóch strumieni GET HTTP oraz deterministyczny test workerów |
| Sprzątanie po zamknięciu, błędzie i anulowaniu | Test DELETE oraz testy `SessionCleanup` z kontrolowanym czasem; brak nowych timerów/wysyłek po zakończeniu |
| Poziom logowania | HTTP przyjmuje logging/setLevel; testy workera obejmują brak ustawienia, próg i równość poziomów oraz format JSON |
| Wyłączenie legacy SSE | HTTP `/sse` i `/message` nie tworzą sesji |
| Tasks opt-in, polling, wynik i błąd narzędzia | Testy HTTP zwykłego wywołania i zadania generowania PRG, dwa warianty wejścia |
| Anulowanie zadania | Długie narzędzie dostępne wyłącznie w fixture testowym otrzymuje anulowany token; polling zwraca cancelled |

## Ograniczenia

Nie wykonywano operacji na fizycznym C64, wdrożenia ani przebudowy obrazu Docker. Anulowanie istniejących operacji urządzenia pozostaje kooperatywne, bez gwarancji przerwania wysłanego polecenia. Magazyn zadań jest pamięciowy, bez trwałości i izolacji użytkowników. Nie dodano nowego subscriptions/listen, cache, MCP Apps ani MRTR — te funkcje należą do następnego planu.

Końcowy przegląd obejmuje `git diff --check`, walidację OpenSPEC oraz zgodność zmian z zachowaniem API C64. Równoległa zmiana adresu urządzenia w `docker-compose.yml` nie jest częścią tej implementacji.

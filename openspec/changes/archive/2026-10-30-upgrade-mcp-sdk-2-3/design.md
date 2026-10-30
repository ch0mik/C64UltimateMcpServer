# Design

## Context

Stan analizy: 2026-10-30. Zgodnie z doprecyzowaniem użytkownika ten etap obejmuje wyłącznie plan i dokumenty OpenSPEC. Implementacja, restore i testy aplikacji pozostają do wykonania.

Serwer ASP.NET Core na .NET 10 używa centralnych wersji pakietów. `Program.cs` jawnie ustawia `Stateless = false`, rejestruje eksperymentalny magazyn Tasks i uruchamia nadawców powiadomień w `RunSessionHandler`. Istnieją testy klienta, zasobów i generatora assembly, ale brak testów negocjacji przez rzeczywisty endpoint MCP.

Lokalne źródła: `C:\Retro\Commodore\c64-voice-controll\csharp-sdk`. HEAD to `a1d3e38a` (`v2.3.0-16-ga1d3e38a`), a wydanie `v2.3.0` wskazuje `9a327829ef066c44a0d737c58bf3c7617d7c701c`. API i wersje zależności sprawdzono także w tagu przez `git show`; zmian po tagu nie należy przypisywać pakietowi 2.3.0. Lokalny SDK jest materiałem referencyjnym, nie zależnością projektu.

### Wpływ kolejnych wydań

| Wydanie | Znaczenie dla tego projektu |
| --- | --- |
| [2.0.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.0.0) | Nowy protokół, bezsesyjny transport domyślny, Tasks przeniesione do osobnego pakietu, deprecated Logging. Zmiana automatycznej serializacji nieobiektowych wyników wymaga testów kontraktowych. |
| [2.1.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.1.0) | Nowy opcjonalny handler `subscriptions/listen`; nie jest automatycznym zamiennikiem obecnych subskrypcji sesyjnych. |
| [2.2.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.2.0) | `HttpServerSessionMode` umożliwia wybraną migrację hybrydową na jednym endpointcie. |
| [2.3.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.3.0) | Poprawki discovery i statusów HTTP, zachowanie kolejności rejestracji, zależności AI 10.11.0/System 10.0.13. `McpClient.ServerInfo` jest nullable; dotyczy przyszłych testów klienta, nie obecnego ustawienia `ServerInfo` serwera. |

Pełna [lista wydań wskazana przez użytkownika](https://github.com/modelcontextprotocol/csharp-sdk/releases). Zmiany OAuth i stdio nie wymagają zmian w obecnym serwerze HTTP bez implementacji klienta OAuth. MCP Apps, Skills, MRTR i nowe strumienie subskrypcji nie są potrzebne do tej migracji.

## Goals / Non-Goals

**Goals:** utrzymanie funkcjonalności dotychczasowych klientów, dostęp do nowego protokołu, prawidłowa rejestracja Tasks i testowalny cykl życia sesji.

**Non-Goals:** zmiana API urządzenia, przebudowa generatorów, nowe narzędzia C64, wdrożenie na urządzeniu, trwały magazyn zadań, wdrożenie uwierzytelniania lub nowych rozszerzeń poza Tasks.

## Decisions

### 1. Wersje pakietów

W `Directory.Packages.props` zaplanowano:

| Pakiet | Obecnie | Docelowo |
| --- | --- | --- |
| ModelContextProtocol | 1.4.0 | 2.3.0 |
| ModelContextProtocol.AspNetCore | 1.4.0 | 2.3.0 |
| ModelContextProtocol.Extensions.Tasks | brak | 2.3.0 |
| Microsoft.Extensions.AI.Abstractions | 10.7.0 | 10.11.0 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.9 | 10.0.13 |

Do projektu serwera dodać `PackageReference` Tasks. Pozostałe pakiety pozostawić w obecnych wersjach. Zweryfikować rzeczywisty graf NuGet po restore, zwłaszcza brak NU1605. Zachować net10.0; migracja nie wymaga kompilowania lokalnego SDK.

### 2. Tryb hybrydowy HTTP

Zastąpić stałą `UseStatefulHttpTransport` jawnym `SessionMode = HttpServerSessionMode.StatefulForInitializeClients`. Samo zachowanie `Stateless = false` wymuszałoby cofanie nowych klientów do initialize i uniemożliwiało korzystanie z nowego Tasks. Pełny stateless z kolei usunąłby dotychczasowe subskrypcje i logowanie sesyjne.

Klienci 2025-11-25 korzystają z initialize, identyfikatora sesji oraz GET/DELETE. Klienci 2026-07-28 używają discovery i żądań bezsesyjnych; nie uruchamiać dla nich okresowych nadawców. Pozostawić `/` oraz `/health`. Legacy `/sse` i `/message` nie będą włączane: README i przykłady korzystają ze Streamable HTTP. Udokumentować tę zmianę dla zewnętrznych klientów, których konfiguracji nie ma w repozytorium.

### 3. Migracja Tasks

Użyć namespace `ModelContextProtocol.Extensions.Tasks` oraz `.WithTasks(new InMemoryMcpTaskStore())`. Usunąć przypisania `options.TaskStore`, `SendTaskStatusNotifications` oraz niepotrzebne suppression MCPEXP001 związane ze starym Tasks. Wyniki i stan pobierać przez nowe API polling. Brak zgodności API/protokołu z eksperymentalnym Tasks 1.x musi być opisany w README.

Magazyn utworzyć raz na proces i przekazać builderowi, żeby stan był dostępny pomiędzy bezsesyjnymi żądaniami. Zwykłe wywołania nie mogą zamieniać się w Tasks bez opt-in. Magazyn pamięciowy zachowuje obecny model pojedynczej instancji; restart usuwa zadania. Nie deklarować trwałości ani izolacji magazynu między użytkownikami. Anulowanie jest kooperatywne: aktualne narzędzia urządzenia nie przyjmują tokenu i nie zapewniają cofnięcia wykonanych efektów.

### 4. Powiadomienia i cykl życia

Zachować `RunSessionHandler` dla sesyjnej ścieżki i lokalne, uzasadnione wyciszenie MCPEXP002. `ConfigureSessionOptions` nie obejmuje sprzątania po zakończeniu sesji. Po `RunAsync` w `finally` zatrzymać i oczekiwać zakończenia uruchomionych nadawców z ograniczonym czasem sprzątania, po czym usunąć wpis subskrypcji. Obsłużyć również częściowy start i anulowanie; nie maskować pierwotnego błędu błędem sprzątania.

Zastąpić literówkę `notifications/resource/updated` stałą SDK oznaczającą `notifications/resources/updated`, preferując typowane parametry. Sprawdzić serializację pól URI/logowania. Zachować obsługę poziomu logowania starszych klientów, wyciszając MCP9005 tylko przy legacy API, z komentarzem. Nie wyciszać globalnie ostrzeżeń migracyjnych. Nie obiecywać sesyjnych subskrypcji ani logowania klientom 2026-07-28; nie reklamować im niezaimplementowanego `subscriptions/listen`.

### 5. Kontrakty i przykłady

Zachować nazwy i argumenty narzędzi, nazwy promptów i URI zasobów. Porównywać katalogi jako zbiory, ponieważ SDK 2.3.0 zmienia kolejność listowania. Generatory już zwracają jawne `CallToolResult`; sprawdzić zachowanie `structuredContent`, treści tekstowej i `isError`. Dla narzędzi zwracających `string` porównać odpowiedzi z bazowym 1.4.0 zamiast zakładać strukturę na podstawie kompilacji.

We wszystkich plikach `.http` zastąpić nieprawidłowy `2025-11-05` prawidłowym `2025-11-25` i dodać nagłówek wersji do kolejnych żądań sesji. Dodać samodzielne przykłady discovery 2026-07-28 i zadania generatora BASIC (opt-in, polling). Operacje na fizycznym urządzeniu nie są częścią automatycznej weryfikacji.

### 6. Testowanie

Rozszerzyć istniejący projekt `C64UltimateClient.Tests` o fixture hosta ASP.NET Core (np. WebApplicationFactory z `Microsoft.AspNetCore.Mvc.Testing` 10.0.13 i publicznym partial `Program`). Testy mają wykonywać rzeczywiste żądania endpointu, a nie rekonstruować osobną konfigurację MCP. Wymusić osobno oba protokoły; automatyczny fallback klienta mógłby ukryć niesprawną ścieżkę modern.

Pokrycie: health, initialize i DELETE, discovery bez sesji, listy narzędzi/promptów/zasobów, odczyt zasobu, pobranie promptu, generowanie BASIC/assembly oraz błąd walidacji. Osobno: izolacja dwóch sesji, subskrypcja/unsubscribe, prawidłowa metoda powiadomienia, zatrzymanie workerów. Użyć kontrolowanego czasu/sygnałów, aby nie opierać testów na losowych logach i długich opóźnieniach. Tasks: capability, zwykłe wywołanie bez opt-in, task i polling do wyniku, błąd narzędzia, anulowanie deterministycznego długiego narzędzia zarejestrowanego wyłącznie w fixture testowym.

## Risks / Trade-offs

- Starsze Tasks nie są zgodne z 2.x → jawna instrukcja migracji; zwykłe wywołania pozostają obsługiwane.
- Klienci korzystający z automatycznego legacy SSE → zmiana adresu na `/` i transportu na Streamable HTTP.
- Nowoczesne HTTP może czekać na pierwszy komunikat przed wysłaniem nagłówków → test timeoutów; dla długich wywołań użycie Tasks lub właściwego timeoutu klienta/proxy.
- Nowa kolejność katalogów → asercje oparte na nazwach/URI zamiast indeksów.
- Sesje i Tasks w pamięci → model jednej instancji oraz utrata stanu po restarcie opisane w dokumentacji.
- Dostępność pakietów i zgodność całego grafu → restore/build są bramką implementacji; nie zostały wykonane w etapie planowania.

## Migration Plan

1. Przed zmianami uruchomić istniejące testy i zebrać kontrakty odpowiedzi wersji 1.4.0 bez urządzenia.
2. Zmienić zależności, rejestrację Tasks i transport jako jeden spójny krok; wykonać restore/build.
3. Dostosować lifecycle/powiadomienia i dodać testy przy odpowiednich zmianach.
4. Uaktualnić przykłady i obie wersje językowe README; wykonać pełną macierz weryfikacji.
5. Po późniejszym zatwierdzeniu wdrożenia przebudować obraz i sprawdzić `/health` oraz oba protokoły. Ten plan nie wykonuje wdrożenia.

Rollback wymaga przywrócenia kodu i pakietów razem albo poprzedniego obrazu. Nie wystarczy obniżenie numerów NuGet po migracji API. Po restarcie klienci muszą ustanowić nowe sesje, a zadania pamięciowe nie są odzyskiwane.

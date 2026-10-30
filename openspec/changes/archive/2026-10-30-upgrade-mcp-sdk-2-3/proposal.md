# Proposal

## Why

Serwer korzysta z MCP SDK 1.4.0. Aktualizacja do wskazanego 2.3.0 wymaga migracji transportu i Tasks, aby obsłużyć nowy protokół oraz zachować działanie dotychczasowych klientów initialize.

## What Changes

- Aktualizacja pakietów MCP do 2.3.0 i bezpośrednich zależności Microsoft.Extensions do wersji wymaganych przez to wydanie.
- Jawny hybrydowy tryb HTTP: sesje dla klientów initialize i obsługa bezsesyjna dla protokołu 2026-07-28.
- **BREAKING**: migracja eksperymentalnych Tasks 1.x na rozszerzenie Tasks 2.x, wymagające nowego protokołu i jawnej zgody klienta w żądaniu.
- Zachowanie katalogów narzędzi, promptów i zasobów oraz odpowiedzi zwykłych wywołań.
- Obsługa powiadomień i sprzątania sesji zgodna z API 2.3.0; aktualizacja przykładów HTTP i testy integracyjne bez sprzętu.
- **BREAKING**: automatyczne endpointy legacy SSE z SDK 1.x (`/sse`, `/message`) zostaną wyłączone zgodnie z domyślnym ustawieniem SDK 2.x; dokumentowana ścieżka to Streamable HTTP pod `/`.

## Capabilities

### New Capabilities

- `mcp-http-compatibility`: negocjacja HTTP, dostęp do istniejących funkcji i cykl życia sesji; pierwsza specyfikacja tych zachowań w repozytorium.
- `mcp-task-execution`: opcjonalne wykonywanie narzędzi przez nowe rozszerzenie Tasks.

### Modified Capabilities

Brak istniejących specyfikacji OpenSPEC.

## Impact

`Directory.Packages.props`, projekt i konfiguracja serwera, nadawcy powiadomień, testy integracyjne, przykłady HTTP i README. Nowa zależność `ModelContextProtocol.Extensions.Tasks`. Migracja zachowuje .NET 10 i nie wymaga uruchamiania operacji na urządzeniu C64.

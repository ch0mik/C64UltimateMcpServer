# Proposal

## Why

Migracja do MCP SDK 2.3.0 zapewniła zgodność protokołów i podstawowe Tasks. Chcemy wykorzystać nowe możliwości do pracy z C64: otrzymywać rzeczywiste zmiany stanu, prowadzić wieloetapowe tworzenie programów i obsługiwać je w interfejsie MCP Apps.

## What Changes

- Strumień `subscriptions/listen` dla klientów 2026-07-28 oraz zasoby aktualnego stanu urządzenia i napędów.
- Jawne wskazówki cache dla statycznej dokumentacji, katalogów i danych zależnych od klienta.
- Tymczasowe artefakty PRG udostępniane jako zasoby z metadanymi, limitem pamięci i terminem ważności.
- Nowy workflow przygotowania programu z MRTR: uzupełnianie parametrów, opcjonalne sampling/roots i oddzielne potwierdzenie wykonania na C64.
- MCP Apps: panel urządzenia oraz podgląd programu i jego wyniku, z pełną alternatywą tekstową.
- Rozwinięcie Tasks: propagacja anulowania do klienta urządzenia, postęp, obsługa input_required i ograniczone przechowywanie zakończonych zadań.
- Dotychczasowe narzędzia, URI dokumentacji i zwykłe wywołania zachowują kontrakty. Nowe workflow nie zmienia semantyki starych poleceń sterowania.

## Capabilities

### New Capabilities

- `c64-live-resources`: obserwowanie stanu urządzenia i napędów przez nowoczesne subskrypcje.
- `mcp-content-caching`: polityka świeżości i zakresu cache odpowiedzi.
- `c64-program-artifacts`: tymczasowe pliki PRG jako zasoby MCP.
- `c64-interactive-workflows`: przygotowanie i uruchamianie programów z interakcjami MRTR.
- `c64-mcp-apps`: panel C64 i podgląd PRG wewnątrz klientów obsługujących Apps.

### Modified Capabilities

- `mcp-task-execution`: dodatkowe wymagania dla postępu, tokenów anulowania, interakcji i retencji zadań; istniejące wymagania pozostają w mocy.
- `mcp-http-compatibility`: legacy subskrypcje przechodzą z okresowych komunikatów na powiadomienia o rzeczywistych zmianach treści.

## Impact

Serwer, klient REST C64, modele wyników, zasoby i testy. Planowana zależność `ModelContextProtocol.Extensions.Apps` 2.3.0; obsługa Apps w tym wydaniu jest eksperymentalna. Zasoby UI HTML/JS będą częścią serwera. Nie jest wymagane aktualizowanie SDK powyżej 2.3.0 ani osobny hosting strony.

Ten etap tworzy wyłącznie plan i dokumenty. Wdrożenie nowych funkcji wymaga kolejnego polecenia implementacji. Poprzednia migracja jest zakończona i opisana w `../archive/2026-10-30-upgrade-mcp-sdk-2-3/verification.md`.

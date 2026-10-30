# Proposal

## Why

Po migracji MCP należy wyrównać pozostałe bezpośrednie zależności do dostępnych stabilnych wydań. Aktualizacja utrzymuje obecne zachowanie aplikacji i testów.

## What Changes

Aktualizacja sześciu wersji w centralnym Directory.Packages.props. MCP pozostaje 2.3.0. Pakiety, dla których NuGet nie wskazuje nowszego stabilnego wydania, zachowują wersje.

## Capabilities

### New Capabilities

Brak.

### Modified Capabilities

Brak zmian wymagań funkcjonalnych. `skip_specs: true` oznacza aktualizację zależności bez zmiany kontraktów.

## Impact

Biblioteki logowania, host testowy ASP.NET Core, kontrolowany czas, mocki i runner testów. Weryfikacja: restore, pełne testy Release, przykłady HTTP i ponowne sprawdzenie nieaktualnych pakietów.

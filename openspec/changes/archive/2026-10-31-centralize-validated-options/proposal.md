# Proposal

## Why

Użytkownik zlecił zaplanowanie migracji całej konfiguracji do IOptions z walidacją. OAuth już stosuje ten wzorzec, ale połączenie Ultimate nadal czyta IConfiguration w kilku miejscach, a parametry operacyjne są rozproszone w kodzie. Błędne wartości powinny zatrzymywać start, zamiast powodować błędy podczas żądań.

## What Changes

- Jeden composition root rejestruje i waliduje wszystkie sekcje aplikacji przez IOptions<T>, IValidateOptions<T> i ValidateOnStart.
- Przenieść połączenie Ultimate, timeouty pobierania, magazyny, workflow, obserwację, cache, preview, powiadomienia, transport i metadane serwera do typowanych opcji.
- Zachować istniejące klucze Ultimate i Mcp:OAuth, domyślne zachowanie, publiczne kontrakty klienta i narzędzi oraz opcjonalne OAuth (domyślnie wyłączone).
- Walidować zależności między sekcjami, typy, zakresy, URL, tablice i nieznane klucze; błędy bez wartości sekretów.
- Utrzymać frameworkowe Options hosta/logowania/Kestrela, opisując ich konfigurację i dodając walidację własnej polityki tylko tam, gdzie jest potrzebna.
- Zaktualizować przykłady JSON/env/CLI/Docker, testy i dokumentację po wdrożeniu.

## Capabilities

### New Capabilities

- `application-configuration`: pełny kontrakt typowanej, walidowanej konfiguracji aplikacji.

### Modified Capabilities

- `mcp-http-compatibility`: zachować zgodność domyślnego trybu hybrydowego, określić konfigurowalne ścieżki, pozostałe tryby sesji i wyłączenie okresowych logów.

- `mcp-content-caching`: TTL dokumentacji staje się konfigurowalny z zachowaniem domyślnej godziny.
- `mcp-task-execution`: pojemność i retencja Tasks stają się konfigurowalne z zachowaniem obecnych wartości domyślnych.

## Impact

Program.cs, UltimateService i fabryka klienta, moduły Features, legacy workers, istniejący moduł OAuth, appsettings, Docker i testy. Biblioteka klienta zachowuje możliwość użycia bez hosta/DI; IOptions jest sposobem konfiguracji serwera, nie obowiązkową zależnością generatorów i samodzielnego klienta.

Wdrożono na polecenie użytkownika 2026-10-31. Wyniki i ograniczenia zawiera verification.md. Nie dodaje nowych tras REST, magazynu trwałego ani runtime zmiany urządzenia.

# Design

## Context

Wersje ustalono 2026-10-30 przez `dotnet list C64UltimateMcpServer.slnx package --outdated --format json`, z repozytorium NuGet.org. Przed zmianą przechodziło 38 testów Release i osiem plików przykładów HTTP.

## Decisions

| Pakiet | Przed | Po |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk | 18.6.0 | 18.11.0 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.13 | 10.0.14 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.13 | 10.0.14 |
| Microsoft.Extensions.TimeProvider.Testing | 10.1.0 | 10.11.0 |
| Moq | 4.20.72 | 4.21.0 |
| xunit.runner.visualstudio | 3.1.5 | 4.0.1 |

Wszystkie zmiany wprowadzić przez centralne zarządzanie wersjami. Nie migrować frameworka xunit 2.9.3 na inny pakiet xunit.v3: są to różne produkty, a obecny xunit jest aktualny. Zgodność nowego adaptera potwierdzić wykonaniem wszystkich istniejących testów, nie samą kompilacją. Informacje o adapterze: https://xunit.net/releases/visualstudio/4.0.1; Moq: https://www.nuget.org/packages/Moq/4.21.0.

## Risks / Trade-offs

Nowy adapter i host testowy mogą zmienić wykrywanie lub uruchamianie testów. Liczba wykonanych testów musi pozostać co najmniej 38 bez pominięć. Nie wymuszać aktualizacji zależności przechodnich przez dodatkowe referencje; podlegają ograniczeniom właścicieli pakietów.

## Migration Plan

Zaktualizować sześć wersji, wykonać restore/testy/przykłady i ponownie zapytać NuGet o aktualizacje. Rollback przywraca sześć poprzednich wersji wraz z restore. Zmiana nie wymaga wdrożenia ani operacji na C64.

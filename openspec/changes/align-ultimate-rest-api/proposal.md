# Proposal

## Why

Porównanie aktualnej dokumentacji REST Ultimate z repozytorium ujawniło brak obsługi nowszych operacji urządzenia oraz luki między klientem C# a narzędziami MCP. Uzupełnienie tych miejsc pomoże także wcześniej zaplanowanemu dashboardowi Apps i workflow programów.

## What Changes

- Konfiguracja hasła urządzenia i wspólna obsługa żądań, odpowiedzi oraz błędów API.
- Odczyt informacji urządzenia i ekranu menu firmware jako nowe narzędzia MCP.
- Klawiatura i joysticki: odczyt stanu, typowane partie zdarzeń i zwalnianie wejść.
- Brakujące operacje binarne, w tym załadowanie PRG bez automatycznego uruchomienia; wystawienie istniejących metod klienta w MCP.
- Walidacja parametrów pamięci, dysków i URL, testy rzeczywistych żądań klienta przez podstawiony HttpMessageHandler.
- Uczciwa dokumentacja zakresu wsparcia zamiast deklarowania pełnego pokrycia API.

## Capabilities

### New Capabilities

- `ultimate-rest-requests`: poprawność i konfiguracja komunikacji klienta z urządzeniem.
- `ultimate-device-inspection`: informacje identyfikacyjne i odczyt menu firmware.
- `ultimate-input-control`: sterowanie wejściami klawiatury i joysticków.
- `ultimate-binary-transfers`: kompletny dostęp do obsługiwanych operacji plikowych przez REST i MCP.

### Modified Capabilities

Brak zmian istniejących specyfikacji głównych. Obecne kontrakty MCP pozostają zgodne; nowe funkcje są dodatkami, a niepoprawne parametry będą odrzucane przed wysłaniem do urządzenia.

## Impact

Klient C#, konfiguracja serwera, nowe metody MCP, testy, README i przykłady. Bez aktualizacji NuGet. Wspólny transport klienta stanowi zależność dla planu `enable-mcp-2-3-features`; szczegóły współpracy opisuje design.md.

Ten etap obejmuje analizę i plan. Nie zmienia kodu aplikacji i nie wywołuje urządzenia.

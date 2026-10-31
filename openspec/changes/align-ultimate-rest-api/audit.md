# Przegląd zgodności REST

Analiza: 2026-10-30. [Dokumentacja wskazana przez użytkownika](https://1541u-documentation.readthedocs.io/en/latest/api/api_calls.html), pomocniczo źródła firmware [machine](https://github.com/GideonZ/1541ultimate/blob/master/software/api/route_machine.cc) i [runners](https://github.com/GideonZ/1541ultimate/blob/master/software/api/route_runners.cc). `latest`/`master` opisują rozwijane API, nie gwarantują dostępności funkcji na urządzeniu użytkownika. Nie odczytywano jego firmware.

| Obszar | Stan w repozytorium | Proponowana aktualizacja |
| --- | --- | --- |
| Network Password | Brak konfiguracji i nagłówka X-Password | Hasło w konfiguracji klienta, wysyłane do urządzenia |
| GET /v1/info | Brak klienta i narzędzia | Typowany odczyt, opcjonalne pola identyfikacyjne |
| GET machine:menu_screen | Brak | Odczyt i dekodowanie menu, jawny brak aktywnego ekranu |
| GET/POST machine:input | Brak | Stan i partie zdarzeń wejścia z lokalną walidacją |
| POST runners:load_prg | Brak | Osobne ładowanie binarne bez run |
| POST modplay/run_crt | Metody klienta są, MCP ich nie wystawia | Narzędzia binarne zgodne z obecnym sposobem przekazywania danych |
| POST drives/{drive}:mount/load_rom | Metody klienta są, MCP ich nie wystawia | Uzupełnienie MCP; mount obecnie wymaga ścieżki urządzenia mimo szerszego opisu README |
| SID z dodatkowym załącznikiem długości utworów | Obecnie tylko dane SID | Opcjonalne rozszerzenie uploadu, zachowanie istniejącego wariantu |
| Błędy HTTP | Pomijane szczegóły errors dla odpowiedzi nie-2xx | Zachować status i szczegóły; odróżniać nieobsługiwaną funkcję od awarii sieci |
| Parametry i zasoby HTTP | Brak centralnej walidacji; odpowiedzi/dokumenty JSON nie są konsekwentnie zwalniane | Wspólny transport i testy graniczne |
| Drives | Surowe JsonElement zachowują dane; osobny DriveInfo nie obejmuje SoftIEC | Nie uznawać tego za utratę danych; dodawać modele pomocnicze tylko bez usuwania surowych pól |
| Connection tools | SetConnection zwraca komunikat, ale nie zmienia klienta | Wyjaśnić zachowanie w README; zmiana konfiguracji w runtime nie jest częścią tej aktualizacji |

Istotne kontrakty z dokumentacji: menu ma 2000 bajtów (dwie macierze 40×25) i odpowiada 404, gdy nie jest dostępne. Input ma ograniczenia 1–64 zdarzeń oraz 4096 bajtów całego JSON; brak sprzętowego wsparcia daje 501. Pamięć nie może przekroczyć granicy adresowej, a query-write mieści maksymalnie 128 bajtów. Dokładne zasady wejść należy utrwalić w fixture zgodnym z dokumentacją podczas implementacji.

Nie potwierdzono błędu kodowania ukośników w ścieżkach `files`: obecne kodowanie całej ścieżki wymaga testu zgodnego z dekoderem firmware, a nie automatycznego uznania za wadę. Dokumentacja powtarza `drives:remove` dla dwóch opisów, dlatego nie należy na tej podstawie wymyślać endpointu unlink. `files:info` jest opisane jako nieukończone — zachować ostrożny opis wsparcia.

## Aktualny status po przeglądzie wydania — 2026-10-31

Tabela powyżej zachowuje historyczny wynik analizy z 2026-10-30. Od tego czasu wdrożono wspólny transport, propagację anulowania, X-Password, kontrolę przekierowań, podstawową klasyfikację HTTP/błędnego JSON i konfigurację Ultimate przez IOptions z walidacją przy starcie. Testy download factory potwierdzają osobny transport bez hasła urządzenia.

Device-info, menu/input, binarne load-without-run i dodatkowe narzędzia MCP transferów pozostają niewdrożone. Pełna macierz błędów/firmware, walidacja argumentów i kodowania URL nadal należą do tego aktywnego planu; nie zamykamy go na podstawie zakończenia zmian MCP/OAuth/Options. Szczegóły zakończonego zakresu: [completion review](../archive/2026-10-31-centralize-validated-options/completion-review.md).

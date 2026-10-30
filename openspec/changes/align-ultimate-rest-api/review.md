# Niezależny review planu

Data: 2026-10-30. Review wykonał subagent `review_api_plan`, na wyraźne polecenie użytkownika. Zakres: dokumenty i specyfikacje align-ultimate-rest-api, powiązanie z enable-mcp-2-3-features, klient C#, narzędzia MCP i dokumentacja API. Bez edycji kodu aplikacji.

## Uwaga P2: przekierowania i X-Password

Reviewer wskazał, że domyślne podążanie za HTTP 30x może przenieść niestandardowy nagłówek hasła na inny origin. Pierwotny plan sprawdzał oddzielne pobieranie plików, lecz pomijał przekierowanie odpowiedzi urządzenia. [Źródło zachowania HttpClientHandler](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.allowautoredirect?view=net-10.0).

Uwaga przyjęta. Dodano wymaganie braku automatycznych przekierowań, scenariusze cross-origin i polecenia zmieniającego stan, decyzję transportową oraz testy 301/302/303/307/308 dla konstruktora i DI. Powiązany plan MCP wykorzystuje tę samą politykę. Jest to poprawka planu; wykonanie testów należy do implementacji.

Reviewer nie znalazł innych istotnych błędów zgodności API ani sprzeczności między planami w sprawdzonym zakresie.

Po ponownym sprawdzeniu poprawek subagent potwierdził zamknięcie uwagi P2 i nie zgłosił dalszych uwag. Walidacja wszystkich aktywnych zmian i specyfikacji OpenSPEC --strict: poprawna.

## Końcowy przegląd dokumentacji i release notes

Po aktualizacji README, RELEASE_NOTES.md i końcowych zadań dokumentacyjnych w obu planach wykonano dodatkowy review subagentem. Potwierdzono zgodność opisu wdrożonych zmian z kodem oraz archiwalnymi wynikami 38/38 testów i 8/8 przykładów. Funkcje planowane oraz brak konfiguracji OAuth są oznaczone poprawnie. Brak istotnych uwag. Testów aplikacji nie powtarzano dla zmian wyłącznie dokumentacyjnych; walidacja OpenSpec i git diff --check przechodzą.

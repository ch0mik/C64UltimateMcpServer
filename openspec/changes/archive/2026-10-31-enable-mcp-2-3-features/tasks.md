# Tasks

Wdrożono na polecenie użytkownika. Dowody: verification.md i review.md. Test hosta dotyczy oficjalnego AppBridge, nie komercyjnych klientów.

## 1. Artefakty i klient urządzenia

- [x] 1.1 Wykorzystać transport i CancellationToken z zależności `align-ultimate-rest-api`, zachowując istniejące konstruktory; test opóźnionego handlera musi wykazać przerwanie żądania i odczytu bez zamiany anulowania na sukces. Potwierdzić udokumentowane zachowanie anulowania w README.
- [x] 1.2 Dodać ProgramArtifactStore z limitami i TTL z design.md; testami kontrolowanego czasu sprawdzić SHA-256, zgodność bajtów, wygaśnięcie, przekroczenie rozmiaru i ewikcję bez ponownego użycia ID.
- [x] 1.3 Dodać zasób `c64://artifacts/{id}` i metadane w nowych wynikach; test resources/read ma zweryfikować MIME, base64, sumę i błąd brakującego/wypartego zasobu. Udokumentować retencję i kontrakt, pozostawiając testy starych generatorów bez zmian ich pól.

## 2. Tasks i postęp

- [x] 2.1 Dodać ograniczony IMcpTaskStore i retencję terminalnych rekordów; testy muszą pokryć limit 256, odmowę przy samych aktywnych zadaniach i wygaśnięcie po 30 minutach. Udokumentować utratę stanu po restarcie.
- [x] 2.2 Przekazać token Tasks do metod urządzenia oraz dodać raportowanie faz w polling; test rzeczywistego HTTP z blokowanym klientem urządzenia musi potwierdzić tasks/cancel, status cancelled i brak kolejnych wywołań urządzenia.
- [x] 2.3 Dodać progress dla długich zwykłych żądań; testy string i numeric token sprawdzają zachowany typ oraz monotoniczny postęp. Dodać przykład i opis różnicy między progress żądania a statusem Tasks.

## 3. Zdarzenia i cache

- [x] 3.1 Dodać snapshoty status/drives i współdzielony obserwator aktywny przy subskrypcjach; test fake urządzenia i zegara sprawdza zmianę danych, przejście offline/online, brak zdarzenia od samego timestamp i zatrzymanie ostatniego listenera. Opisać URI i pola JSON.
- [x] 3.2 Zarejestrować subscriptions/listen z poprawnym acknowledged, subscriptionId, ograniczoną kolejką i capability; testy dwóch strumieni HTTP obejmują filtrowanie, nieobsługiwane URI, anulowanie i odbiorcę wolniejszego od producenta. Dodać wykonywalny przykład modern.
- [x] 3.3 Podłączyć legacy subscribe/unsubscribe do tego samego źródła zmian; zaktualizować testy i README, potwierdzając izolację sesji oraz brak okresowych powiadomień dla niezmienionej dokumentacji.
- [x] 3.4 Dodać politykę ICacheableResult według tabeli design.md; testy raw HTTP potwierdzają TTL/scope, brak nowych pól w legacy, prywatność katalogów zależnych od capability i poprawną relację TTL artefaktu do wygaśnięcia. Dodać tabelę polityki do dokumentacji.

## 4. MRTR

- [x] 4.1 Dodać ledger operacji i wersjonowanie rund z limitami oraz atomowym confirmed → executing; testy muszą odrzucać zmienione parametry, cel, wygasły stan i podwójne równoległe użycie potwierdzenia bez drugiego efektu.
- [x] 4.2 Dodać ultimate_program_workflow: dane wejściowe, elicitation, kompilacja BASIC/assembly i artefakt; testy incomplete → retry → complete i pełnych argumentów bez interakcji muszą przechodzić. Opisać argumenty, diagnostykę i schemat odpowiedzi.
- [x] 4.3 Dodać opcjonalne sampling i roots zgodnie z capability; testy fake klienta obejmują generowanie źródła, odmowę, brak capability, błędne źródło i ścieżkę klienta niedostępną na serwerze. Dodać przykłady pełnego round-trip.
- [x] 4.4 Dodać potwierdzenie wykonania związane z artefaktem i celem; testy akceptacji, odmowy, cancel, wygasłego/wypartego artefaktu i niepewnego wyniku HTTP muszą wykazać co najwyżej jedno wysłanie polecenia.
- [x] 4.5 Połączyć workflow z Tasks input_required/tasks/update i starszym resolverem zgodności; testy obu ścieżek obejmują zgromadzenie danych, potwierdzenie, wynik, anulowanie i klienta bez capability. Uzupełnić przykłady i dokumentację kompatybilności.

## 5. MCP Apps

- [x] 5.1 Dodać pakiet Apps 2.3.0, moduł rejestracji i zasoby HTML/JS z przypiętym mostem hosta; testy sprawdzają MIME, _meta.ui, visibility, capability i lokalne suppression MCPEXP003. Udokumentować eksperymentalny status API.
- [x] 5.2 Zbudować ultimate_dashboard z wersją, dostępnością i napędami; test UI z fake mostem obejmuje odczyt, refresh i błąd urządzenia, a test narzędzia porównuje wynik tekstowy bez Apps z structuredContent. Dodać opis obsługiwanych hostów.
- [x] 5.3 Zbudować ultimate_program_preview z metadanymi, diagnostyką i hex 256 bajtów; testy obejmują wygasły artefakt, tekst z HTML, brak bezpośrednich połączeń z urządzeniem i brak wykonania przy renderowaniu.
- [x] 5.4 Połączyć akcję wykonania w UI z potwierdzanym workflow i input_required; testy mostu wykazują obsługę accept/decline/cancel, a zwykły klient nadal otrzymuje potrzebne dane bez HTML. Udokumentować ograniczenia CSP i działania app-only.

## 6. Weryfikacja integracji

- [x] 6.1 Uruchomić pełne testy Release i zestaw przykładów offline; porównać bazowe kontrakty z 1.4.0 jako zachowane kontrakty istniejących narzędzi oraz jawnie dozwolone nowe pozycje katalogów, bez ignorowania zmian ich parametrów.
- [x] 6.2 Przeprowadzić test Apps w rzeczywistym kompatybilnym hoście, zapisać nazwę/wersję i wyniki dashboard/preview/workflow; pozostałe scenariusze sprzętowe opisać oddzielnie od automatycznych fake testów.
- [x] 6.3 Sprawdzić każdy scenariusz siedmiu delta specs, wykonać `openspec validate enable-mcp-2-3-features --strict`, przejrzeć końcowy diff i zapisać raport weryfikacji z ograniczeniami.
- [x] 6.4 Zaktualizować README, przykłady MD, specyfikacje OpenSpec i RELEASE_NOTES.md zgodnie z wdrożonym zakresem; wykonać niezależny review subagentem, uwzględnić uwagi i dostarczyć surowy Markdown release notes w końcowej odpowiedzi. Zweryfikować, że funkcje planowane i ograniczenia hostów nie są przedstawione jako działające.

## Workflow follow-up

- Po wykonaniu i przeglądzie zarchiwizować zmianę oraz zaktualizować specyfikacje główne.
- Wdrożenie i ewentualne testy na fizycznym C64 wykonać jako oddzielny etap.

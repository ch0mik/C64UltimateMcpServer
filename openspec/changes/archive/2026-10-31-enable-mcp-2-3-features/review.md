# Independent review — 2026-10-31

Reviewer: subagent `/root/review_api_plan`, requested explicitly by the user. Reviewed MRTR/Tasks, Apps packaging, client transport and live resource lifecycle.

Four findings were corrected and covered by regression tests:

1. Redacting raw JSON could corrupt successful data for short passwords. Parse original data, redact error strings/logs only; tests use `8` and `version`.
2. Blank initial language/source prevented elicited answers from being used. Treat blank strings as missing and normalize language.
3. Idle legacy listeners could keep an unnecessary device poll alive. Track interest in the specific URI and notify the hub on unsubscribe/dispose.
4. Valid JSON with an invalid root/error shape could escape the observer and stop hosting. Convert malformed shapes to UltimateClientException/unavailable snapshots; verify recovery.

The reviewer performed a second review and confirmed all four findings closed, with no new significant issues in the checked fixes. Final Release run passed 71/71 tests after the fixes.

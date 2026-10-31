# Independent implementation review — 2026-10-31

Reviewer: subagent `/root/review_feature_coverage`, read-only. Reviewed validators and section shapes, startup order/snapshots, consumers, SDK adapter, routing/OAuth, client ABI and test coverage.

Two P2 findings were fixed:

1. ConfigurationBinder could ignore a scalar where an object/array was expected, potentially leaving OAuth disabled. Recursive ValidateShape now rejects invalid structures, including mixed scalar/children and scalar arrays; safe errors do not echo values. Added regression cases.
2. The old UltimateService IConfiguration constructor ignored Downloads.RequestTimeout. It now uses shared binding/validation through LegacyConfigurationAdapter.Downloads; regression verifies invalid settings are not ignored.

An additional lifecycle issue was addressed: ValidateOnStart uses IOptionsMonitor rather than the separate IOptions.Value cache. A first hosted-service initializer captures all application options before workers. Test reload before first cache/preview call confirms the startup snapshot.

The reviewer rechecked the fixes and initializer, confirmed both findings closed and reported no new significant problem or required fix. Full final Release suite passed 169/169. Provider/proxy deployment and hardware tests remain outside this change.

## Follow-up completeness review

User requested a final comparison against all completed tasks. The same independent reviewer found a P2 evidence gap: valid maxima, systematic outside-boundary/overflow and ClockSkew tests were missing despite task 1.4 being checked. Added a matrix of all 13 numeric and 13 duration settings (accepted boundaries and rejected adjacent values), numeric/time overflow and actual JWT ClockSkew behavior tests. Also tightened icon image MIME, numeric pixel dimensions and URL control-character validation with regressions.

Re-review confirmed task 1.4 closed and no new significant issue or blocker in the checked changes. Final tests: 236/236; subsequent HTTP/UI/browser and publish checks passed. This completed scope excludes the remaining separate align-ultimate-rest-api plan.

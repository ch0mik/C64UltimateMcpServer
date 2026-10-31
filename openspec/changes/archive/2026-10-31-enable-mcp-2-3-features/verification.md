# Verification — 2026-10-31

Implemented the full MCP 2.3 feature change, preserving existing tool contracts and explicitly adding three model-facing tools plus one Apps-only helper. The SDK reference was tag v2.3.0, not the newer local checkout HEAD.

## Results

- `dotnet test C64UltimateMcpServer.slnx -c Release --no-restore`: **71/71 passed** (10 assembly, 61 client/server).
- `scripts/Test-McpExamples.ps1`: **10/10 HTTP files passed**, six legacy and four modern, without hardware.
- `npm test`: **4/4 UI tests passed**.
- `npm run test:browser`: dashboard and preview passed initialization, rendering and refresh; preview also passed execution confirmation handoff. Official `@modelcontextprotocol/ext-apps` **2.0.4** AppBridge/PostMessageTransport reference host, Microsoft Edge **156.0.4314.50**. Fake device, zero device executions and no external UI requests.
- `dotnet publish ... -c Release --no-restore -o tmp/features-publish`: passed; both generated HTML bundles present under Features/Ui/dist.
- `openspec validate --all --strict`: passed before archive. Archive completed, synchronizing five new and two existing specifications; final all-spec validation passed 8/8 items. `git diff --check` passed.

## Scenario coverage

| Delta specification | Evidence |
| --- | --- |
| c64-program-artifacts | FeatureStoreTests: immutable bytes, checksum, bounds, TTL/eviction; HTTP artifact blob round-trip and missing IDs |
| c64-live-resources | FeatureStoreTests and McpFeatureIntegrationTests: snapshots, change suppression, offline/recovery, malformed data, listen ack/filter, bounded/coalesced queues, disconnect cancellation |
| mcp-content-caching | Raw HTTP modern and legacy tests: cache fields, private catalogs/live data, public docs and remaining artifact TTL |
| c64-interactive-workflows | MRTR rounds, blank parameters, roots/sampling, missing capabilities, accept/decline, expiry, replay, changed args/target, concurrent at-most-once execution, transport uncertainty |
| mcp-task-execution | Store retention/capacity, input_required/update, cancellation while waiting and during device HTTP; numeric/string progress |
| c64-mcp-apps | Metadata/MIME/visibility and text fallback tests, four UI units, two actual browser reference-host paths |
| mcp-http-compatibility | Legacy SDK resolver, unchanged existing schemas/results, modern discovery/Tasks, real change notifications with session isolation |

DeviceRedirectTests verifies 301/302/303/307/308 on local same/cross origins, with no second command or forwarded credentials. Client tests verify cancellation, old compiled signatures, password redaction without success-data corruption and malformed JSON handling.

## Limits

One process, memory-only artifacts/tasks/continuations; restart loses state. No user isolation or MCP OAuth configured. Apps APIs are experimental. Product hosts such as ChatGPT/Claude were not tested; reference-host confirmation handoff is not proof a product host will accept it. Device-info/menu/input and remaining binary API additions belong to the separate REST change. No deployment or physical C64 testing. Existing CS1591 library documentation warnings remain.

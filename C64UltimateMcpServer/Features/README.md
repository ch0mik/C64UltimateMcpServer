# MCP 2.3 feature guide

The server uses MCP SDK 2.3.0 and supports legacy 2025-11-25 and modern 2026-07-28 HTTP clients. These additions preserve existing tool argument/result contracts. There are 49 model-facing tools and one Apps-only helper, 15 prompts, 55 documentation resources, two live resources, two UI resources and an artifact resource template.

## Program workflow and MRTR

Call `ultimate_program_workflow` with the following arguments:

| Argument | Meaning |
| --- | --- |
| `language` | `basic` or `assembly` |
| `source` | Source content, up to 1 MiB; never a server path |
| `generationRequest` | Optional sampling prompt, up to 64 KiB |
| `execute` | Defaults to false; true requests separate confirmation |
| `selectProject` | Requests optional client roots as context |
| `artifactId` | Reuses an existing temporary PRG |

For an offline compile use `{"language":"basic","source":"10 END"}`. Results contain text and structured content with artifact metadata or diagnostics. Assembly includes a BASIC loader. Existing generators continue returning base64 unchanged.

Missing input uses form elicitation when advertised by the client. Generation requires sampling capability; project selection requires roots capability. Client roots are hints, not paths opened on the server. Unsupported interactions return a structured error so clients can supply complete arguments explicitly.

Modern MRTR returns input requests and an opaque `requestState`. Retry the same tool with **identical original arguments**, the latest `requestState`, and the requested `inputResponses` key. See [the executable MRTR example](../../../examples_http/modern/04_program_mrtr.http). Legacy clients use the SDK compatibility resolver and server-to-client requests. Workflow Tasks expose `input_required`; answer through `tasks/update` and poll for the next state/result.

Execution requires an accepted confirmation bound to the artifact SHA-256 and configured device target. Decline/cancel performs no device action. Continuations are single-use, expire within one hour and reject changed arguments, target or replay. The ledger holds at most 256 operations and allows eight interaction rounds. A transport failure after sending returns an unconfirmed execution result; the server never automatically repeats the device command. Cancellation cannot undo a command already received by hardware.

## Temporary PRG artifacts

Read `c64://artifacts/{id}` for `application/octet-stream` blob content (base64 in the protocol). Metadata includes ID, URI, SHA-256, byte size, load address, creation and expiry. Preview shows at most 256 bytes of hex. Artifacts are immutable, expire after one hour and are bounded to 4 MiB per artifact, 16 MiB and 256 entries total. Expired and then oldest entries are evicted. Missing/expired IDs return an error.

## Live resources and cache

`c64://device/status` reads API version and availability; `c64://device/drives` reads drive data. JSON fields are `available`, `observedAt`, `data`, `error`. No CPU state is inferred. Device errors and malformed responses become unavailable snapshots.

A shared observer polls every five seconds only while live URIs have listeners. Modern `subscriptions/listen` first acknowledges supported URIs, then emits changes with the original `_meta.subscriptionId`. Legacy subscribe/unsubscribe uses the same observer. Timestamp-only changes do not trigger notifications. Queues are bounded and updates coalesced; disconnect/unsubscribe removes interest and cancels an outstanding poll when no listener needs that URI. Documentation does not emit artificial periodic updates.

Cache hints are emitted only for modern protocol clients:

| Result | TTL | Scope |
| --- | --- | --- |
| Embedded documentation | 1 hour | public |
| Live resources | 0 | private |
| Artifact content | Remaining artifact lifetime | private |
| Tool/prompt/resource/template catalogs | 0 | private |
| UI resources | 0 | private |

## Tasks and progress

Tasks remain opt-in. The process-local store holds 256 records, keeps terminal results for 30 minutes and does not evict active tasks to make space. `tasks/cancel` reaches the workflow and device HTTP token. Polling reports phase/status; ordinary workflow calls with a progress token receive monotonic progress preserving numeric or string token types.

SDK 2.3.0 attribute-based Tasks cannot directly compose MRTR. A dedicated workflow task runner handles input requests and `tasks/update` before the SDK Tasks filter. Other tools retain SDK task handling.

## MCP Apps

`ultimate_dashboard` shows availability, API version and drives. `ultimate_program_preview` shows artifact metadata and bounded hex. Both provide text/structured results without Apps. UI resources are `ui://c64/dashboard.html` and `ui://c64/program-preview.html`, MIME `text/html;profile=mcp-app`.

The bundled official Apps bridge uses host MCP calls; it never connects directly to the device. CSP grants no external connect/resource domains and values render as text. The Apps-only `ultimate_request_program_execution` helper returns a confirmation handoff to the host conversation. It does not execute the program: the host must complete the explicit workflow confirmation. Unsupported/refused host actions are shown in the UI. The helper is hidden from clients without the UI capability and has app-only visibility.

Apps APIs are experimental (MCPEXP003). Browser integration was verified with the official `@modelcontextprotocol/ext-apps` 2.0.4 AppBridge reference host and Microsoft Edge 156.0.4314.50, covering dashboard, preview, refresh and confirmation handoff. This does not certify ChatGPT, Claude or other product hosts. See [UI build and tests](Ui/README.md).

## Configuration and operational limits

Use `Ultimate:BaseUrl` and optional `Ultimate:NetworkPassword` (`Ultimate__NetworkPassword` as an environment variable). The password is applied as device `X-Password`; redirects are disabled. Caller cancellation propagates through HTTP and response reads. Existing client constructors and compiled method signatures remain available.

Artifacts, workflow continuations and Tasks are process-local and lost on restart. Run a single trusted instance; cache scope and random IDs do not provide user authorization or tenant isolation. Optional MCP OAuth is available through validated IOptions settings and remains disabled by default; see [configuration](../Configuration/README.md). Additional device-info, menu/input and binary API tools remain in the separate Ultimate REST API plan. No deployment or physical C64 testing was performed.

All operational values described above are the defaults. Configure artifact/Task/workflow limits, polling, cache TTL, preview, notifications and transport through the [validated Options settings](../Configuration/README.md). The configured continuation deadline is also bounded by artifact expiry, and all application settings remain fixed until restart.

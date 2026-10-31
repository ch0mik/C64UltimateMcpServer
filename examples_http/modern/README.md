# Modern MCP HTTP examples

These examples use protocol **2026-07-28** and Streamable HTTP at `/`, without initialize or `Mcp-Session-Id`.

1. Run `01_discovery.http` to inspect discovery and list tools.
2. Run the first request in `02_basic_task.http`, copy `result.taskId` into `@taskId`, then repeat the polling request until completion. The completed result contains `result.structuredContent.prgDataBase64`. Responses may be JSON or SSE `data:` events.

Tasks require the per-request extension opt-in shown in `_meta`. The `Mcp-Name` header for `tasks/get` is the task ID. To cancel a running task, use `tasks/cancel` with the same headers, metadata, and `taskId`. Ordinary calls omit the extension capability and return their result directly.

The first four files work without a C64 device. `03_program_artifacts.http` compiles and reads a PRG artifact; `04_program_mrtr.http` completes an interactive compile. Keep original arguments unchanged and copy the newest requestState for each manual continuation.

`05_live_subscriptions.http` is a long-lived manual stream for status/drives changes; cancel it to disconnect. It polls device reads and is excluded from the default offline suite.

The default suite runs six legacy smoke files plus four modern files (ten total). The offline verification script executes their requests and fills in the task ID automatically:

```powershell
./scripts/Test-McpExamples.ps1 -BaseUrl http://localhost:8080
```

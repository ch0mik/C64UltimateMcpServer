# Modern MCP HTTP examples

These examples use protocol **2026-07-28** and Streamable HTTP at `/`, without initialize or `Mcp-Session-Id`.

1. Run `01_discovery.http` to inspect discovery and list tools.
2. Run the first request in `02_basic_task.http`, copy `result.taskId` into `@taskId`, then repeat the polling request until completion. The completed result contains `result.structuredContent.prgDataBase64`. Responses may be JSON or SSE `data:` events.

Tasks require the per-request extension opt-in shown in `_meta`. The `Mcp-Name` header for `tasks/get` is the task ID. To cancel a running task, use `tasks/cancel` with the same headers, metadata, and `taskId`. Ordinary calls omit the extension capability and return their result directly.

Both files work without a C64 device. The offline verification script executes their requests and fills in the task ID automatically:

```powershell
./scripts/Test-McpExamples.ps1 -BaseUrl http://localhost:8080
```

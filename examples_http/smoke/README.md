# Smoke Scenarios

Run these first after `docker compose up --build`.

Order:

1. `01_mcp_initialize.http`
2. `02_mcp_tools_list.http`
3. `03_mcp_prompts_list.http`
4. `04_mcp_prompts_get.http`
5. `05_mcp_resources_list.http`
6. `06_mcp_resources_read.http`
7. `07_ultimate_version.http`
8. `08_ultimate_connection.http`
9. `09_ultimate_drives.http`

Notes:

- Every file is self-contained and performs its own MCP handshake.
- These requests are non-destructive and are safe as the default manual verification set.
- If the Ultimate device is offline, only the first six MCP-only files are expected to pass.

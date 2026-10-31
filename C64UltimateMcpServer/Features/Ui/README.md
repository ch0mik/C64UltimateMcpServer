# MCP Apps UI

The source uses the official `@modelcontextprotocol/ext-apps` bridge pinned to 2.0.4. Generated `dist/dashboard.html` and `dist/program-preview.html` are self-contained and committed; .NET publish includes them and runtime needs no Node.js or CDN.

```powershell
npm ci
npm run build
npm test
```

Browser verification uses `playwright-core` with an installed Microsoft Edge and the official AppBridge reference host. Start the Release MCP server at `http://127.0.0.1:18085`, configured with `Ultimate:BaseUrl=http://127.0.0.1:18086`, then run:

```powershell
npm run test:browser
```

The test starts its own fake device and local reference host. It checks both sandboxed Apps, refresh, confirmation handoff, no external requests and no device execution. It does not test a commercial product host or send commands to a physical C64. Four Node unit tests cover rendering, host calls, refusal and unsafe text. Build output includes dependency license notices. Generated bundles must be rebuilt after source changes.

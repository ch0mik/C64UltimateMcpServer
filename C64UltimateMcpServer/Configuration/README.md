# Application configuration

All application-owned settings use typed `IOptions<T>`, safe strict binding, `IValidateOptions<T>` and `ValidateOnStart`. Every application options value is captured before hosted workers start, including features invoked lazily. Changes require restart. Scalar values in place of sections, malformed types, unknown keys and inconsistent limits stop startup; diagnostics do not echo supplied values.

## Settings reference

See [appsettings.json](../appsettings.json) for the complete default configuration. Durations use the .NET TimeSpan format (`hh:mm:ss`, optional fractional seconds).

| Section | Settings / defaults | Validation |
| --- | --- | --- |
| `Ultimate` | `BaseUrl=http://192.168.0.120`, optional `NetworkPassword`, `RequestTimeout=00:00:30` | HTTP/HTTPS origin with root path, no userinfo/query/fragment; printable ASCII password; timeout 100 ms–10 min |
| `Downloads` | `RequestTimeout=00:01:40` | 100 ms–10 min; separate HTTP transport without device credentials |
| `Mcp:Artifacts` | `MaxEntries=256`, `MaxBytes=16777216`, `MaxArtifactBytes=4194304`, `Lifetime=01:00:00` | Entries 1–10000; item 2 bytes–64 MiB, total 2 bytes–512 MiB; item <= total; TTL 1 s–24 h |
| `Mcp:Tasks` | `Capacity=256`, `TerminalRetention=00:30:00`, `PollInterval=00:00:00.250` | Capacity 1–10000, retention 1 s–7 days; poll 10 ms–1 min and below retention |
| `Mcp:Workflow` | `Capacity=256`, `ContinuationLifetime=01:00:00`, `MaxRounds=8`, `MaxSourceBytes=1048576`, `MaxGenerationRequestBytes=65536`, `SamplingMaxTokens=4096`, `MaxRootHints=20` | Capacity 1–10000, TTL 1 s–24 h and <= artifact TTL, rounds 1–64, source 1 byte–16 MiB, prompt 1 byte–1 MiB, tokens 1–32768, roots 1–1000 |
| `Mcp:LiveResources` | `PollInterval=00:00:05`, `ListenerQueueCapacity=32` | Poll 100 ms–1 h, queue 2–4096 |
| `Mcp:Cache` | `DocumentationTimeToLive=01:00:00` | 0–24 h; private/live cache policy remains fixed |
| `Mcp:Apps` | `PreviewBytes=256` | 1–256 bytes |
| `Mcp:Notifications` | `LegacyLoggingEnabled=true`, `LegacyLoggingInterval=00:00:15`, `CleanupTimeout=00:00:05` | Times 100 ms–5 min; interval applies when enabled; cleanup <= host shutdown timeout |
| `Mcp:Transport` | `EndpointPath=/`, `HealthPath=/health`, `SessionMode=StatefulForInitializeClients`, `IdleTimeout=02:00:00`, `MaxIdleSessionCount=10000` | Literal local noncolliding paths; named SDK modes only; idle 1 s–7 days, idle count 1–100000 |
| `Mcp:Server` | `Name`, `Version`, `Title`, `Description`, `WebsiteUrl`, `Icons` | Name/version required; optional website and icon sources HTTPS without control characters; concrete image MIME without wildcard/parameters; theme and ASCII pixel dimensions checked |
| `Mcp:OAuth` | `Enabled=false`, `Authority`, `Audience`, `RequiredScopes=["mcp:access"]`, `ClockSkew=00:00:30` | Conditional OAuth rules below; enabled clock skew 0–5 min |
| `Host` | Native `HostOptions`, including `ShutdownTimeout` (runtime default 30 s) | Shutdown timeout 100 ms–1 day; use this section for an explicit override |

Framework `Logging`, `Kestrel`, `AllowedHosts`, `urls` / `ASPNETCORE_URLS` and environment selection retain their native ASP.NET Core configuration and validation. Logging-level reload remains available. Docker port mappings, healthcheck timing, build/test configuration and Node test environment variables remain owned by their respective tools. Protocol constants, resource URIs, crypto parameters and tool request arguments are not host settings.

## JSON, environment and CLI

Existing keys are preserved. `Ultimate__BaseUrl` maps to `Ultimate:BaseUrl` and is consumed through `IOptions<UltimateClientOptions>.Value.BaseUrl` by both legacy tools and new workflows.

```powershell
$env:Ultimate__BaseUrl = "http://192.168.0.120"
$env:Ultimate__RequestTimeout = "00:00:20"
$env:Mcp__LiveResources__PollInterval = "00:00:02"
dotnet run --project C64UltimateMcpServer -- --Mcp:Apps:PreviewBytes=128
```

Provider precedence remains the host default: base JSON, environment JSON, development secrets where enabled, environment variables, then CLI. For example `--Ultimate:BaseUrl=http://device.example` overrides JSON/env. Docker uses the same double-underscore variables; device passwords should be injected separately, never committed.

The standalone `Ultimate64Client` retains its constructors and does not require DI. `UltimateClientOptions.RequestTimeout` controls its timeout unless an explicit constructor timeout is supplied. Compatibility server constructors bind/validate through a shared adapter. `ultimate_set_connection` keeps its existing informational behavior; changing the device requires configuration and restart.

## Transport behavior

The default hybrid mode serves both protocol versions. `Stateful` serves legacy initialize sessions and rejects modern discovery with UnsupportedProtocolVersion. `Stateless` serves modern requests without transport sessions; legacy initialization does not create a persistent session. Session GET/DELETE and unsolicited legacy session features are unavailable there. Deprecated `/sse` is not enabled by this migration.

Configured paths must start with `/`, be literal normalized paths, and avoid query/fragment, percent-encoded paths, wildcards, route parameters, dot segments, metadata prefixes and MCP/health route collisions. Use `/` or omit a trailing slash. Changing `EndpointPath` moves the protected MCP routes; `HealthPath` remains public. Disabling legacy logging removes only its sender, preserving resource subscription workers.

Documentation cache TTL is configurable; privacy scope, zero TTL for live data/catalogs, artifact TTL bounded by remaining lifetime and legacy wire compatibility are preserved. Workflow deadlines never extend beyond the configured operation lifetime or artifact expiry. Source limits apply to initial arguments, elicitation and sampling.

## Optional MCP OAuth


MCP OAuth is **disabled by default**. Configuration binds to `IOptions<McpOAuthOptions>`, uses `IValidateOptions<McpOAuthOptions>` and `ValidateOnStart`, and is resolved before the request pipeline starts. Unknown properties inside `Mcp:OAuth` are rejected. Settings are a process-lifetime snapshot: restart after changing them.

The server validates JWT access tokens issued by an external OAuth/OIDC provider. It does not issue tokens, host a login page or need a client secret. Clients acquire tokens from the configured provider.

## Configuration

Default configuration preserves anonymous access:

```json
{
  "Mcp": {
    "OAuth": {
      "Enabled": false,
      "Authority": "",
      "Audience": "",
      "RequiredScopes": ["mcp:access"]
    }
  }
}
```

Enable protection with your provider and the public MCP resource URL:

```json
{
  "Mcp": {
    "OAuth": {
      "Enabled": true,
      "Authority": "https://identity.example.com/realms/c64",
      "Audience": "https://c64-mcp.example.com/",
      "RequiredScopes": ["mcp:access"]
    }
  }
}
```

Equivalent environment overrides:

```powershell
$env:Mcp__OAuth__Enabled = "true"
$env:Mcp__OAuth__Authority = "https://identity.example.com/realms/c64"
$env:Mcp__OAuth__Audience = "https://c64-mcp.example.com/"
$env:Mcp__OAuth__RequiredScopes__0 = "mcp:access"
```

Array configuration merges by numeric index across .NET providers. Replace each relevant index when overriding an existing list; unused lower-priority entries remain part of the effective configuration. The options class starts with an empty array to avoid appending defaults during binding; shipped appsettings supplies `mcp:access`.

## Validation and access behavior

When enabled, authority and audience must be absolute HTTPS URLs without credentials, query or fragment. Audience is also the advertised public resource identifier. Scopes must be a nonempty list of unique, case-sensitive OAuth scope tokens (ASCII printable characters excluding spaces, quotes and backslashes). Validation messages identify keys without echoing configured values. Invalid values stop startup; there is no anonymous fallback. Disabled OAuth does not require these values, although malformed types and unknown keys still fail binding.

JWT validation requires a signed, unexpired token from the configured issuer with the expected audience. Signing keys come from the authority's OIDC discovery/JWKS metadata; HTTPS metadata is required. Lifetime validation uses `ClockSkew` (default 30 seconds, range zero to five minutes when enabled). Every configured scope is required, parsed from space-separated `scope` and/or `scp` claims using ordinal comparison.

With OAuth enabled, all MCP routes use the same access policy, including modern discovery, tools/resources/tasks, legacy initialize, GET streams and DELETE sessions. The configured health endpoint (default `/health`) remains public. Missing/invalid tokens return 401 with `resource_metadata` and `scope`. Valid tokens missing required scopes return 403 with `error="insufficient_scope"` and the required scope list.

Resource metadata is public at `/.well-known/oauth-protected-resource` for a root resource. For audience `https://c64-mcp.example.com/c64/mcp`, it is at `/.well-known/oauth-protected-resource/c64/mcp`. Challenges use the configured public HTTPS audience's origin, even if a reverse proxy connects to the application over HTTP. Configure the proxy to route both the MCP endpoint and this metadata path to the application; a public subpath must be rewritten/mapped to the configured MCP endpoint (default `/`). The implementation does not trust arbitrary forwarded headers. With OAuth disabled, metadata requests return 404.

## Scope and limitations

This is optional resource-server JWT authentication, not a complete identity platform. No opaque-token introspection, token issuance, dynamic client registration, Enterprise Managed Authorization or separate read/write roles are added. All authorized users share the existing process-local artifacts, tasks and workflow state; no tenant isolation is introduced.

`Ultimate:NetworkPassword` remains an independent device REST credential. OAuth access tokens are never used as the device password. No real identity-provider deployment, key-rollover integration or physical C64 tests were performed; automated tests use signed test tokens and static provider metadata.

## Completeness review

The final review covers all 12 application-owned option sections, their consumers, framework boundaries and startup lifetime. Numeric and duration limits have tests for valid minimum/maximum and immediately outside each boundary, plus integer/TimeSpan overflow. JWT tests prove configured ClockSkew changes acceptance of an expired token. Invalid icon MIME/size/URL formats have regression tests. The 236-test Release suite, 10 HTTP examples, four UI tests and both reference-host Apps scenarios pass. See [scope and evidence](../../openspec/changes/archive/2026-10-31-centralize-validated-options/completion-review.md).

# Design

Use IOptions<McpOAuthOptions>, a registered IValidateOptions validator, strict binding and ValidateOnStart. Resolve the validated snapshot during pipeline construction; changes require restart. Disabled options do not require issuer/audience/scopes. Enabled options require HTTPS authority and public resource audience URLs without credentials, query or fragment, plus unique nonempty RFC6749 scope tokens.

Use the external authority for OIDC metadata/signing-key discovery. Validate JWT signature, issuer, audience and lifetime. Advertise the same audience as RFC8707 resource metadata via the SDK AddMcp handler. All configured scopes are required (ordinal comparison), accepted from space-separated scope or scp claims. Protect all MCP routes, including discover and legacy sessions, using one policy. Health stays anonymous; disabled OAuth metadata returns 404. No automatic fallback after configuration failure.

The server is a resource server; clients acquire JWT access tokens from an external provider. It does not issue tokens or support opaque-token introspection. State remains shared and memory-only; authentication does not introduce per-user isolation. Existing legacy API contracts remain unchanged.

Tests use local signing keys and static issuer metadata, never a real provider or hardware. Validate disabled/default behavior, startup errors without secret values, metadata challenge, signature/issuer/audience/lifetime checks, missing scopes and legacy authorization. Finish with independent review, docs and English release notes.

## Review decisions

The SDK builds default challenges from the incoming request origin. To preserve the configured public HTTPS URL behind an HTTP proxy hop, route challenge and forbid to JWT events with a public metadata URI derived from Audience. Keep the SDK metadata handler's match URI relative, avoiding origin checks against the internal HTTP connection. Include insufficient_scope and the required scopes in 403 responses. Tests cover exact public URL and audience paths, metadata over internal HTTP, all-scope enforcement and legacy GET/DELETE.

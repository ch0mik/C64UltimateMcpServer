# mcp-oauth-configuration Specification

## Purpose
Provides optional OAuth JWT protection for MCP endpoints through typed startup-validated configuration, scope enforcement and public resource discovery while preserving anonymous access when disabled.

## Requirements

### Requirement: Typed startup configuration
The server SHALL bind Mcp:OAuth to IOptions<McpOAuthOptions> and validate it before serving requests. Enabled SHALL default to false. When enabled, Authority and Audience SHALL be valid absolute HTTPS URLs and RequiredScopes SHALL be nonempty unique valid OAuth scope tokens. Invalid configuration SHALL stop startup without logging supplied secret values.

#### Scenario: Invalid enabled settings
- **WHEN** OAuth is enabled without valid authority, audience or scopes
- **THEN** startup fails with configuration-key validation errors and does not allow anonymous fallback

### Requirement: Optional MCP access protection
When OAuth is enabled the server SHALL require a valid signed unexpired JWT with the expected issuer, audience and every configured scope on all MCP endpoints. When disabled it SHALL preserve anonymous MCP access. Health SHALL remain public.

#### Scenario: Disabled OAuth
- **WHEN** OAuth is disabled and issuer/audience are absent
- **THEN** anonymous MCP requests work and OAuth metadata is not advertised

#### Scenario: Invalid or insufficient token
- **WHEN** an enabled server receives an invalid token or a valid token missing required scopes
- **THEN** it responds with 401 or 403 respectively before executing an MCP handler, and a 403 response includes a Bearer insufficient_scope challenge listing the required scopes

### Requirement: OAuth resource discovery
When enabled the server SHALL publish MCP protected-resource metadata with the configured audience, authority and scopes and include resource_metadata in authentication challenges.

#### Scenario: Discover protected resource
- **WHEN** a client requests MCP without a token while OAuth is enabled
- **THEN** the 401 challenge points to publicly readable resource metadata describing the configured authority and resource

#### Scenario: TLS reverse proxy
- **WHEN** the application receives an internal HTTP request while Audience identifies a public HTTPS resource with a path
- **THEN** authentication challenges advertise the public HTTPS metadata URL for that path and the metadata remains retrievable through the configured proxy route

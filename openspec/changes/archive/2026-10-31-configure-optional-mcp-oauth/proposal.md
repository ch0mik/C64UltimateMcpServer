# Proposal

## Why

User requested optional OAuth2, configured through IOptions with validation. Enabled authentication must protect the MCP endpoint; invalid settings must not silently permit anonymous access.

## What Changes

- Add Mcp:OAuth options (Enabled=false, Authority, Audience, RequiredScopes) and conditional startup validation.
- Integrate JWT bearer authentication, required scope policy and MCP protected-resource metadata.
- Preserve anonymous MCP when disabled and keep health public.

## Capabilities

### New Capabilities

- `mcp-oauth-configuration`: typed optional OAuth resource-server configuration and enforcement.

## Impact

Server configuration, JWT Bearer dependency, tests and documentation. No authorization server, token issuance, client registration, role model or per-user store isolation. No changes to the device NetworkPassword or existing Ultimate configuration in this change.

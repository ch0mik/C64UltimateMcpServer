# MCP SDK 2.3.0 Features and Validated Configuration

## Added

- Interactive BASIC and assembly workflow with MRTR input collection, optional sampling and project roots, and explicit confirmation before device execution.
- Temporary PRG resources with SHA-256 metadata, expiry and bounded memory storage.
- MCP Apps dashboard and program preview, bundled locally with text fallbacks and a host-mediated execution confirmation handoff.
- Live device status and drive resources with modern subscription streams and change-driven legacy notifications.
- Bounded Tasks with interactive updates, phase reporting, cancellation and terminal-result retention.
- Protocol-aware cache hints for documentation, catalogs, live resources and artifacts.
- Optional OAuth JWT protection with scope enforcement, protected-resource metadata and public HTTPS authentication challenges. OAuth remains disabled by default.
- Device X-Password configuration and redirect protection.

## Configuration

- Centralized application settings in typed IOptions with strict binding, startup validation and fixed startup snapshots.
- Preserved Ultimate__BaseUrl, device password settings and existing public client constructors.
- Added configurable device/download timeouts, storage limits, workflow deadlines/input limits, polling, queues, cache TTL, preview size, notifications, transport routes/session modes and server identity.
- Added configurable OAuth clock skew with JWT behavior tests.
- Rejected malformed section shapes, unknown keys, numeric/time overflow and inconsistent settings without exposing configured values in errors.
- Validated icon URLs, concrete image MIME types and pixel dimensions.
- Application settings require restart; native ASP.NET Core hosting configuration and logging-level reload remain available.

## Reliability and Compatibility

- Preserved existing tool input schemas and result formats.
- Prevented replayed confirmations from repeating device execution.
- Hardened malformed device responses, password redaction and subscription cleanup.
- Added offline HTTP examples for artifacts and MRTR.

## Validation

- 236 Release tests and 10 offline HTTP example files passed.
- Four UI tests and both Apps browser scenarios passed using the official AppBridge 2.0.4 reference host in Microsoft Edge 156.0.4314.50, including a custom MCP route and preview size.
- Publish packaging, independent completeness review and OpenSpec validation completed.

## Limitations and Remaining Plans

Artifacts, Tasks and workflow state are memory-only and lost on restart. Apps APIs remain experimental; commercial MCP hosts were not tested. OAuth requires an external JWT/OIDC provider and does not add token issuance, opaque-token introspection, Enterprise Managed Authorization or per-user state isolation.

Additional Ultimate REST device-info, menu/input, load-without-run and binary MCP tools remain in a separate, incomplete OpenSpec plan. The completed scope does not claim to use every optional or client-only SDK API.

No deployment, physical C64 testing or real identity-provider/proxy integration was performed. Existing CS1591 XML documentation warnings remain in the libraries.

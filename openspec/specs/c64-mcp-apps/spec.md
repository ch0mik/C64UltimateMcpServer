# c64-mcp-apps Specification

## Purpose
Udostępnia panel urządzenia i podgląd programu wewnątrz klienta MCP Apps, zachowując możliwość pracy przez zwykłe wyniki narzędzi.

## Requirements

### Requirement: Device dashboard
The server SHALL provide `ultimate_dashboard` associated with `ui://c64/dashboard.html`, showing availability, API version, drives, observation time, and explicit refresh or error state.

#### Scenario: Open dashboard
- **WHEN** an Apps-capable client invokes the dashboard
- **THEN** it can render the associated UI resource using structured device data without the browser contacting the device directly

### Requirement: Program preview
The server SHALL provide `ultimate_program_preview` associated with `ui://c64/program-preview.html`, showing artifact metadata, diagnostics, and a bounded hex preview without automatically executing the PRG.

#### Scenario: Inspect artifact
- **WHEN** a client previews an existing artifact
- **THEN** the UI displays its size, checksum, load address, and expiry and leaves execution to a separate confirmed workflow

### Requirement: UI capability fallback
The server SHALL advertise appropriate UI metadata only where supported, serve resources as `text/html;profile=mcp-app`, and preserve useful text and structured results for clients without Apps support.

#### Scenario: Text-only client
- **WHEN** a client without UI capability invokes dashboard or preview
- **THEN** it receives the same relevant data in an ordinary tool result and is not required to render HTML

### Requirement: UI communication boundary
UI resources SHALL use the negotiated host bridge for MCP calls, escape untrusted content, declare restrictive resource policies, and limit any app-only tools to the intended UI interactions.

#### Scenario: Malicious program text
- **WHEN** source or diagnostics contain HTML or script markup
- **THEN** the preview renders it as text and does not execute the markup

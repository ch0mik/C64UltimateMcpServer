## Purpose

Zapewnia dostęp do funkcji serwera C64 przez Streamable HTTP dla klientów starszego i nowego protokołu MCP.

## ADDED Requirements

### Requirement: Legacy initialize sessions
The server SHALL support initialize-based clients using protocol 2025-11-25 at `/`, with independent session identifiers and session termination.

#### Scenario: Initialize and delete session
- **WHEN** a client initializes using protocol 2025-11-25
- **THEN** the server returns a session identifier accepted by subsequent requests and permits deleting that session

### Requirement: Modern discovery
The server SHALL support protocol 2026-07-28 discovery and requests at `/` without requiring or issuing a session identifier.

#### Scenario: Discover without initialize
- **WHEN** a client discovers the server using protocol 2026-07-28
- **THEN** it can list tools without an initialize handshake or session identifier

### Requirement: Existing server primitives
The server SHALL preserve existing tool names, prompt names, resource URIs, and ordinary tool response content across supported protocol paths.

#### Scenario: Offline primitives on both protocol paths
- **WHEN** either client lists primitives, reads `c64://basic/spec`, retrieves a prompt, and generates a BASIC PRG
- **THEN** the requests succeed without a C64 device and return the existing content formats

### Requirement: Legacy resource subscriptions
The server SHALL isolate resource subscriptions by session, emit `notifications/resources/updated` for subscribed URIs, and stop session workers when a session ends.

#### Scenario: Two independent subscribers
- **WHEN** one of two legacy sessions subscribes to a resource
- **THEN** only that session receives periodic update notifications for that URI

#### Scenario: Unsubscribe and cleanup
- **WHEN** the subscriber unsubscribes and later deletes its session
- **THEN** subsequent notification cycles omit the URI and its session workers stop

### Requirement: Health endpoint
The server SHALL expose `/health` independently of MCP session negotiation.

#### Scenario: Health request
- **WHEN** an HTTP client requests `/health`
- **THEN** it receives HTTP 200 and the existing health message

### Requirement: Streamable HTTP endpoint policy
The server SHALL use `/` for MCP transport and SHALL NOT expose legacy SSE sessions through `/sse` or `/message` by default.

#### Scenario: Legacy SSE connection attempt
- **WHEN** a client tries to open a legacy SSE session at `/sse`
- **THEN** it cannot establish that session and the documented migration path is Streamable HTTP at `/`

### Requirement: Legacy logging
The server SHALL support logging-level selection and log notifications for legacy sessions, and SHALL NOT start periodic session notification workers for modern stateless requests.

#### Scenario: Legacy client selects logging level
- **WHEN** a legacy client selects a logging level
- **THEN** it receives only log messages meeting that threshold

#### Scenario: Stateless request completes
- **WHEN** a modern request completes
- **THEN** no periodic logging or resource-notification worker remains for that request

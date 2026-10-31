## MODIFIED Requirements

### Requirement: Legacy initialize sessions
With the default StatefulForInitializeClients mode or explicitly selected Stateful mode, the server SHALL support initialize-based clients using protocol 2025-11-25 at the configured MCP endpoint (default `/`), with independent session identifiers and session termination. Explicit Stateless mode SHALL NOT create persistent transport sessions and SHALL NOT expose session GET/DELETE operations.

#### Scenario: Initialize and delete session
- **WHEN** a client initializes using protocol 2025-11-25 in default hybrid or Stateful mode
- **THEN** the server returns a session identifier accepted by subsequent requests at the configured endpoint and permits deleting that session

#### Scenario: Explicit Stateless mode
- **WHEN** Stateless mode is configured and a legacy initialize request is processed
- **THEN** it does not create a persistent transport session and standalone session GET/DELETE are unavailable

### Requirement: Modern discovery
With the default StatefulForInitializeClients mode or explicitly selected Stateless mode, the server SHALL support protocol 2026-07-28 discovery and requests at the configured MCP endpoint (default `/`) without requiring or issuing a session identifier. Explicit Stateful mode SHALL report the SDK UnsupportedProtocolVersion outcome for modern protocol requests rather than falsely advertise native modern support.

#### Scenario: Discover without initialize
- **WHEN** a client discovers the server using protocol 2026-07-28 in default hybrid or Stateless mode
- **THEN** it can list tools without an initialize handshake or session identifier at the configured endpoint

#### Scenario: Explicit Stateful mode
- **WHEN** a modern client attempts discovery against a server configured as Stateful
- **THEN** it receives the SDK UnsupportedProtocolVersion outcome and can use legacy initialization where supported by that client

### Requirement: Legacy resource subscriptions
In modes providing legacy sessions, the server SHALL isolate resource subscriptions by session, emit notifications/resources/updated only when a subscribed resource changes, and stop session workers when a session ends. Stateless mode SHALL NOT start persistent legacy session subscription workers.

#### Scenario: Two independent subscribers
- **WHEN** one of two legacy sessions subscribes to a resource and its content changes
- **THEN** only that session receives an update notification for that URI

#### Scenario: Unsubscribe and cleanup
- **WHEN** the subscriber unsubscribes and later deletes its session
- **THEN** subsequent notification cycles omit the URI and its session workers stop

#### Scenario: Unchanged documentation
- **WHEN** a subscribed embedded document remains unchanged
- **THEN** polling intervals alone do not cause update notifications

### Requirement: Health endpoint
The server SHALL expose the configured health endpoint (default `/health`) independently of MCP session negotiation and authentication, returning the existing health message.

#### Scenario: Health request
- **WHEN** an HTTP client requests the configured health endpoint without an MCP session or OAuth token
- **THEN** it receives HTTP 200 and the existing health message

### Requirement: Streamable HTTP endpoint policy
The server SHALL use the validated configured MCP endpoint (default `/`) and SHALL NOT enable deprecated legacy SSE sessions through /sse or /message as part of the configuration migration. Explicitly configured endpoint routes SHALL preserve OAuth enforcement on all generated MCP routes while health and resource metadata remain public.

#### Scenario: Legacy SSE connection attempt
- **WHEN** a client tries to open a deprecated legacy SSE session
- **THEN** it cannot establish that session and the documented migration path is the configured Streamable HTTP endpoint

#### Scenario: Configured MCP endpoint
- **WHEN** EndpointPath is changed to `/mcp` and OAuth is enabled
- **THEN** MCP POST and supported session GET/DELETE operations at `/mcp` require the same authorization policy and `/` no longer serves MCP

### Requirement: Legacy logging
In modes providing legacy sessions, the server SHALL preserve SDK logging-level selection and, when LegacyLoggingEnabled is true (default), periodic log notifications respecting that threshold. When disabled it SHALL NOT create a periodic logging sender. Modern stateless requests SHALL NOT start periodic session notification workers.

#### Scenario: Legacy client selects logging level
- **WHEN** a legacy client selects a logging level and periodic logging is enabled
- **THEN** it receives only periodic log messages meeting that threshold

#### Scenario: Logging disabled
- **WHEN** LegacyLoggingEnabled is false while legacy resource subscriptions are active
- **THEN** no periodic logging sender runs and resource subscriptions continue independently

#### Scenario: Stateless request completes
- **WHEN** a modern request completes
- **THEN** no periodic logging or resource-notification worker remains for that request

# mcp-task-execution Specification

## Purpose
Umożliwia klientom MCP wykonywanie narzędzi jako zadań i odbieranie ich wyników przez rozszerzenie Tasks nowego protokołu.

## Requirements

### Requirement: Task capability discovery
The server SHALL advertise `io.modelcontextprotocol/tasks` to clients using protocol 2026-07-28.

#### Scenario: Discover task support
- **WHEN** a modern client discovers the server
- **THEN** the response includes the Tasks extension capability

### Requirement: Explicit task opt-in
The server SHALL create tasks only for modern requests explicitly opting into the Tasks extension and SHALL return ordinary tool results for requests without that opt-in.

#### Scenario: Ordinary tool call
- **WHEN** a client calls `ultimate_generate_basic_prg` without task opt-in
- **THEN** it receives the tool result directly with content and structured PRG data

#### Scenario: Task-backed tool call
- **WHEN** a modern client calls `ultimate_generate_basic_prg` with Tasks opt-in
- **THEN** it receives a task identifier and can poll `tasks/get` to obtain the completed tool result

### Requirement: Tool error results
The server SHALL preserve `isError` and structured error details when a task completes with a tool-level error.

#### Scenario: Invalid program input
- **WHEN** a task invokes the PRG generator with input rejected by its validator
- **THEN** its completed result contains the same error fields as the equivalent ordinary tool call

### Requirement: Task cancellation
The server SHALL expose cancellation for active tasks and report a cancelled terminal state without promising rollback of device operations already performed.

#### Scenario: Cancel a running task
- **WHEN** a client cancels an active task
- **THEN** subsequent polling reports cancellation and cancellation is propagated to handlers that accept a cancellation token

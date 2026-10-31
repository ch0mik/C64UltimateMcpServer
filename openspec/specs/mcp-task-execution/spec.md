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

### Requirement: Device operation cancellation
Task cancellation SHALL propagate through server handlers and pending device HTTP requests; already committed device actions SHALL be reported without claiming rollback.

#### Scenario: Cancel pending upload
- **WHEN** a client cancels a task whose device HTTP request is still pending
- **THEN** the HTTP request receives cancellation and the task reaches its cancelled state

### Requirement: Operation progress
Task polling SHALL expose the current workflow phase in status messages, and ordinary long-running requests with a progress token SHALL emit progress while preserving that token's JSON type.

#### Scenario: Numeric progress token
- **WHEN** a direct request uses a numeric progress token
- **THEN** progress notifications contain the same numeric token and report monotonic work progress

#### Scenario: Task-backed workflow
- **WHEN** compilation, upload, or execution advances to another phase
- **THEN** subsequent task polling reports that phase without relying on a completed HTTP request's notification channel

### Requirement: Task input requests
Task-backed interactive workflows SHALL enter input_required while client input is pending and resume from tasks/update responses without repeating completed device effects.

#### Scenario: Confirmation during task execution
- **WHEN** a task reaches the execution-confirmation phase
- **THEN** polling exposes the input request and acceptance or decline submitted through tasks/update determines the outcome

### Requirement: Bounded task retention
The server SHALL retain terminal tasks for the validated configured retention period (default 30 minutes), admit at most the configured capacity (default 256 stored tasks), and explicitly reject new task creation if capacity cannot be freed without removing active tasks.

#### Scenario: Capacity exhausted by active work
- **WHEN** all configured task slots are occupied by active tasks
- **THEN** a new task request receives an explicit capacity error and existing tasks remain accessible

#### Scenario: Terminal task expires
- **WHEN** a client polls a task after its configured retention period
- **THEN** it receives the documented task-not-found result and storage is reclaimed

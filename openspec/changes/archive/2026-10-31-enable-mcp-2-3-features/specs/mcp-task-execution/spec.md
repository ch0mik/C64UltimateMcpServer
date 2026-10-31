## ADDED Requirements

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
The server SHALL default to retaining terminal tasks for 30 minutes, admit at most 256 stored tasks, and explicitly reject new task creation if capacity cannot be freed without removing active tasks.

#### Scenario: Capacity exhausted by active work
- **WHEN** all task slots are occupied by active tasks
- **THEN** a new task request receives an explicit capacity error and existing tasks remain accessible

#### Scenario: Terminal task expires
- **WHEN** a client polls a task after its retention period
- **THEN** it receives the documented task-not-found result and storage is reclaimed

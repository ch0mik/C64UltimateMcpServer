## MODIFIED Requirements

### Requirement: Bounded task retention
The server SHALL retain terminal tasks for the validated configured retention period (default 30 minutes), admit at most the configured capacity (default 256 stored tasks), and explicitly reject new task creation if capacity cannot be freed without removing active tasks.

#### Scenario: Capacity exhausted by active work
- **WHEN** all configured task slots are occupied by active tasks
- **THEN** a new task request receives an explicit capacity error and existing tasks remain accessible

#### Scenario: Terminal task expires
- **WHEN** a client polls a task after its configured retention period
- **THEN** it receives the documented task-not-found result and storage is reclaimed

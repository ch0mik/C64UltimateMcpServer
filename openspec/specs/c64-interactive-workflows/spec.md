# c64-interactive-workflows Specification

## Purpose
Prowadzi klienta przez przygotowanie i opcjonalne wykonanie programu C64, zbierając brakujące dane w kolejnych rundach jednego logicznego workflow.

## Requirements

### Requirement: Program preparation workflow
The server SHALL provide `ultimate_program_workflow` to collect language, source or generation request, and execution intent, compile BASIC or assembly, and return an artifact with diagnostics.

#### Scenario: Missing program parameters
- **WHEN** a capable client starts the workflow without required parameters
- **THEN** the server returns an incomplete result requesting the missing values and resumes from valid input responses

### Requirement: Optional client sampling and roots
The workflow SHALL request sampling or client roots only when the request needs them and the client advertises the corresponding capability; client-local files SHALL be supplied as content rather than opened as server-local paths.

#### Scenario: Generate source with client sampling
- **WHEN** a client requests code generation and advertises sampling
- **THEN** the server obtains a proposal through an input request and compiles the returned source with diagnostics

#### Scenario: Client-local project
- **WHEN** a client selects a root on its own machine
- **THEN** the workflow requests source content and does not assume that the path exists on the server

### Requirement: Confirmed execution and replay handling
The workflow SHALL execute a compiled artifact only after explicit confirmation bound to that artifact and target device, and SHALL prevent continuation retries from executing the same confirmed operation twice.

#### Scenario: Decline execution
- **WHEN** the user declines or cancels execution confirmation
- **THEN** the artifact remains available until expiry and no device execution occurs

#### Scenario: Replayed confirmation
- **WHEN** the client resends an already consumed continuation
- **THEN** the server returns the recorded outcome or a replay error without another device command

### Requirement: Capability fallback and continuation validation
The workflow SHALL reject modified, expired, or mismatched continuation state and SHALL give clients without required interaction capabilities a complete list of explicit arguments needed to continue manually.

#### Scenario: Unsupported interactive client
- **WHEN** a client cannot handle a required input request
- **THEN** it receives actionable structured guidance without a hanging interaction or device side effect

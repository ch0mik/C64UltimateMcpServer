## Purpose

Zapewnia spójne wysyłanie żądań do Ultimate, konfigurację dostępu i czytelne błędy przy zachowaniu zgodności klienta C#.

## ADDED Requirements

### Requirement: Optional device password
The client SHALL send a configured network password as X-Password on every device request, omit it when absent, and exclude it from logs, MCP schemas, and results.

#### Scenario: Protected device
- **WHEN** the application is configured with a device password
- **THEN** JSON and binary device requests include the header while responses and logs do not disclose its value

### Requirement: No automatic device redirects
The device transport SHALL NOT automatically follow HTTP redirects, including when supplied through dependency injection, and SHALL report a redirect without forwarding credentials or resubmitting the device action.

#### Scenario: Redirect to another origin
- **WHEN** a device request receives a redirect to another host, port, or scheme
- **THEN** no request is sent to the redirected location and X-Password stays within the original request

#### Scenario: Redirect of a state-changing request
- **WHEN** a device command receives a redirect, including within the same origin
- **THEN** it returns a redirect error without following it or automatically repeating the command

### Requirement: Meaningful device errors
The client SHALL preserve HTTP status and available API error details, distinguish authorization and unsupported-operation failures, and avoid reporting an unconfirmed device action as successful.

#### Scenario: Unsupported input hardware
- **WHEN** the device reports an unsupported operation
- **THEN** the caller receives a non-retryable capability error with the original status and error details

#### Scenario: Power-off disconnect
- **WHEN** the connection closes before a power-off response confirms the outcome
- **THEN** the caller receives an explicitly unconfirmed outcome without automatic resubmission

### Requirement: Parameter validation
The client SHALL reject invalid memory ranges, malformed hexadecimal data, unsupported enumerated values, and invalid disk-image parameters before sending a device request.

#### Scenario: Memory boundary
- **WHEN** the requested memory operation would cross the end of addressable C64 memory
- **THEN** it fails locally and sends no device request

### Requirement: Cancellation and transport lifetime
The client SHALL propagate cancellation to pending device HTTP operations, release response resources, and preserve existing constructor and method usage while allowing an injected transport.

#### Scenario: Cancel read
- **WHEN** a caller cancels a pending device read
- **THEN** the request and response-body read are cancelled without being converted into a success result

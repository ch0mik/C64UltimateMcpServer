# c64-live-resources Specification

## Purpose
Udostępnia aktualny stan C64 i napędów oraz pozwala obserwować zmiany bez utrzymywania sesji transportowej nowego protokołu.

## Requirements

### Requirement: Device state resources
The server SHALL expose `c64://device/status` and `c64://device/drives` as JSON snapshots with observation time, availability, and either device data or an explicit error.

#### Scenario: Device unavailable
- **WHEN** a client reads a live resource while the device is unreachable
- **THEN** it receives an unavailable snapshot with error details rather than stale data presented as current

### Requirement: Modern subscription acknowledgement
The server SHALL accept `subscriptions/listen` for supported resource URIs, send exactly one acknowledgement before events, and tag each event with the originating subscription ID.

#### Scenario: Mixed subscription request
- **WHEN** a client requests supported and unsupported notification types or URIs
- **THEN** the acknowledgement lists only the supported subset and subsequent events belong to that subset

### Requirement: Change-driven delivery and cleanup
The server SHALL deliver resource updates only to listeners of the changed URI, detect availability transitions, and release listener state when its stream is cancelled.

#### Scenario: Two listeners
- **WHEN** two clients observe different resource URIs and only the device status changes
- **THEN** only the status subscriber receives an update and unchanged snapshots cause no repeated update

#### Scenario: Disconnect
- **WHEN** the final listener disconnects
- **THEN** its stream and notification work stop and subscription-driven device polling stops

## Purpose

Zapewnia bezpośrednie sterowanie udokumentowanym wejściem klawiatury i joysticków Ultimate bez zastępowania go zapisem do pamięci C64.

## ADDED Requirements

### Requirement: Input state and batches
The server SHALL expose `ultimate_get_input_state`, `ultimate_send_input_events`, and `ultimate_release_inputs`, returning the resulting device input state after a successful write.

#### Scenario: Type a key and hold joystick
- **WHEN** a client submits a valid keyboard/joystick event batch
- **THEN** its order and transitions are preserved in one device request and the resulting state is returned

### Requirement: Atomic local validation
The client SHALL validate the complete input batch against documented kinds, names, counts, ports, transitions, event restrictions, and serialized byte limits before any device request.

#### Scenario: Invalid later event
- **WHEN** a batch contains a valid first event and an invalid subsequent event
- **THEN** the whole batch is rejected locally with no partial transmission

#### Scenario: Byte limit independent of event count
- **WHEN** the event count is valid but the final UTF-8 JSON body exceeds its allowed size
- **THEN** the request is rejected without silent splitting or truncation

### Requirement: Explicit release and unsupported hardware
The server SHALL offer explicit release of REST-injected inputs and report unsupported input hardware without falling back to memory manipulation or pretending that input was applied.

#### Scenario: Release held inputs
- **WHEN** the client requests release-all
- **THEN** the server sends the documented release event and returns the resulting state

#### Scenario: Unsupported product
- **WHEN** the device rejects the input API as unsupported
- **THEN** the tool returns NotSupported with the device error and no alternative side-effecting operation occurs

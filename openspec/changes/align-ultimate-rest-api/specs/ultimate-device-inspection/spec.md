## Purpose

Umożliwia identyfikowanie urządzenia i zdalne odczytywanie menu firmware, przy czytelnym rozróżnieniu niedostępnego ekranu i braku obsługi API.

## ADDED Requirements

### Requirement: Device information
The server SHALL expose `ultimate_get_device_info` with product and available firmware, FPGA, core, host, and device-identity metadata, tolerating omitted optional fields.

#### Scenario: Optional identity missing
- **WHEN** the device omits its unique ID or a product-specific version field
- **THEN** the tool returns the remaining data without inventing a value or failing deserialization

### Requirement: Firmware menu screen
The server SHALL expose `ultimate_get_menu_screen` with validated raw matrices, decoded printable text, colour attributes, and explicit firmware-menu semantics, without toggling the menu during a read.

#### Scenario: Menu visible
- **WHEN** the device returns a valid menu screen
- **THEN** the tool returns its raw bytes and a bounded decoded representation preserving non-text glyph information

#### Scenario: Menu unavailable
- **WHEN** the device reports no readable active menu screen
- **THEN** the tool returns a structured MenuUnavailable result with device error details and does not open the menu automatically

#### Scenario: Invalid payload
- **WHEN** the binary screen does not match the documented matrix layout
- **THEN** the tool rejects it as an invalid device response instead of producing a partial screen

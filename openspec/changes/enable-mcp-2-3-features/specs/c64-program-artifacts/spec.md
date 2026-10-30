## Purpose

Pozwala pobierać, sprawdzać i ponownie wykorzystywać wygenerowany PRG przez identyfikator zasobu zamiast kopiowania dużych ciągów base64 w rozmowie.

## ADDED Requirements

### Requirement: Artifact creation and reading
The server SHALL create temporary PRG artifacts at `c64://artifacts/{id}` with byte size, SHA-256, load address, creation time, expiry time, and base64 file content readable through MCP resources.

#### Scenario: Compile a BASIC program
- **WHEN** the new program workflow compiles valid BASIC
- **THEN** it returns an artifact URI whose bytes and checksum match the generated PRG

### Requirement: Bounded retention
The server SHALL default to a 60 minute artifact lifetime, 4 MiB per artifact, 16 MiB total, and 256 entries, evict expired entries first, then oldest entries when capacity is needed.

#### Scenario: Oversized artifact
- **WHEN** a generated file exceeds the per-artifact limit
- **THEN** creation fails with a structured size-limit error and no partial artifact becomes readable

#### Scenario: Expired or evicted artifact
- **WHEN** a client reads an expired, evicted, or unknown identifier
- **THEN** it receives an explicit unavailable-artifact error and cannot read another artifact by reusing that identifier

### Requirement: Existing generator compatibility
Existing generator tools SHALL preserve their direct base64 results; artifact URIs SHALL be introduced through new workflows and preview tools.

#### Scenario: Existing generator caller
- **WHEN** a client calls `ultimate_generate_basic_prg` directly
- **THEN** it continues receiving the existing `prgDataBase64` and associated result fields

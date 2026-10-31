## MODIFIED Requirements

### Requirement: Static documentation freshness
For protocol 2026-07-28 the server SHALL advertise public cache scope and the validated configured documentation TTL, defaulting to 3600000 ms, for unchanged embedded documentation read results.

#### Scenario: Repeated documentation reads
- **WHEN** a modern client reads `c64://basic/spec` with default settings
- **THEN** it receives the original contents with public cache scope and a 3600000 ms TTL

#### Scenario: Configured documentation TTL
- **WHEN** a modern client reads embedded documentation with a valid nondefault DocumentationTimeToLive
- **THEN** its response advertises that TTL without changing privacy policy for live resources, catalogs or artifacts

## Purpose

Ogranicza powtarzane pobieranie dokumentacji, jednocześnie zachowując właściwą świeżość danych urządzenia i rozdzielenie danych klientów.

## ADDED Requirements

### Requirement: Static documentation freshness
For protocol 2026-07-28 the server SHALL advertise public cache scope and a 3600000 ms TTL for unchanged embedded documentation read results.

#### Scenario: Repeated documentation reads
- **WHEN** a modern client reads `c64://basic/spec`
- **THEN** it receives the original contents with public cache scope and the documented TTL

### Requirement: Client-dependent response policy
The server SHALL mark capability-filtered catalogs as private with TTL zero, live snapshots as private with TTL zero, and artifact responses as private with TTL no greater than remaining artifact lifetime.

#### Scenario: Apps-specific catalog
- **WHEN** two clients receive different tool visibility based on UI capabilities
- **THEN** neither catalog is advertised as safe for shared caching across those clients

### Requirement: Cache invalidation and legacy compatibility
The server SHALL emit the appropriate resource invalidation event when served dynamic content changes and SHALL omit new cache fields for legacy protocol responses.

#### Scenario: Cached content changes
- **WHEN** a subscribed resource changes before a client's TTL expires
- **THEN** its update notification invalidates the previous cached content

#### Scenario: Legacy resource read
- **WHEN** a 2025-11-25 client reads documentation
- **THEN** its content contract remains compatible with the existing server

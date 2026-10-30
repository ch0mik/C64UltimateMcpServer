## Purpose

Udostępnia klientom MCP brakujące warianty operacji binarnych Ultimate, zachowując znaczenie ładowania, uruchamiania i montowania plików.

## ADDED Requirements

### Requirement: Complete binary tool access
The server SHALL expose binary load-PRG, play-MOD, run-CRT, mount-disk, and load-drive-ROM tools using the corresponding documented REST operations.

#### Scenario: Client method already exists
- **WHEN** a client invokes one of the new binary tools
- **THEN** it uses the existing compatible REST client method where available rather than substituting a filesystem command

### Requirement: Load without run
The binary PRG load tool SHALL load the program without requesting automatic execution and SHALL disclose the device reset associated with loading.

#### Scenario: Prepare a PRG
- **WHEN** a client invokes `ultimate_load_prg_binary`
- **THEN** it sends a binary load request, not a run request, and returns the load outcome

### Requirement: Binary validation and attachments
Binary tools SHALL reject malformed or empty input, preserve uploaded bytes and required file/type metadata, and preserve device error details in structured MCP error results.

#### Scenario: Invalid disk source
- **WHEN** an upload lacks valid binary data or the metadata needed to identify its format
- **THEN** it fails before a device request and describes the missing or invalid input

### Requirement: Optional SID companion attachment
SID binary playback SHALL accept an optional companion song-length attachment while preserving the existing single-file invocation and song selection.

#### Scenario: Two-file SID request
- **WHEN** a client provides SID bytes and companion data
- **THEN** both are sent as separate attachments with the selected song parameter preserved

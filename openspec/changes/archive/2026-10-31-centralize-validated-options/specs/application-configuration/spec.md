## ADDED Requirements

### Requirement: Typed configuration ownership
All application-owned runtime settings listed in inventory.md SHALL be configured through typed IOptions with one composition root. Business services SHALL NOT read IConfiguration or environment variables directly. Framework and build/tool settings SHALL retain their native configuration owners and be documented in the coverage inventory.

#### Scenario: Device client creation
- **WHEN** any server workflow or legacy tool creates a device client
- **THEN** it uses the same validated Ultimate configuration snapshot without a separate raw configuration fallback

### Requirement: Complete startup validation
Every application options section SHALL use strict binding and startup validation for type, range, unknown keys and the cross-section constraints in design.md. Invalid configuration SHALL fail before requests or device observers start and SHALL NOT expose secret values.

#### Scenario: Invalid lazy feature settings
- **WHEN** the artifact budget is invalid even if no artifact tool has been called
- **THEN** host startup fails with a configuration-key error before serving requests

#### Scenario: Inconsistent retention settings
- **WHEN** MaxArtifactBytes exceeds MaxBytes or workflow lifetime exceeds artifact lifetime
- **THEN** startup fails instead of changing or silently ignoring the configured values

### Requirement: Predictable configuration lifetime and defaults
Application options SHALL use process-lifetime snapshots with documented restart semantics, preserve existing defaults and Ultimate/OAuth keys, and keep OAuth disabled by default. Framework logging reload SHALL retain its native behavior.

#### Scenario: Configuration changes during confirmation
- **WHEN** configuration is changed after startup while a workflow waits for confirmation
- **THEN** the running instance retains the original validated target and settings until restart

### Requirement: Effective operational settings
Validated options SHALL control the actual stores, deadlines, polling, queues, cache TTL, preview size, notifications, transport, identity and HTTP timeouts. Input limits SHALL apply to initial arguments and MRTR responses. Workflow deadlines SHALL remain bounded by both configured operation lifetime and artifact expiry.

#### Scenario: Nondefault settings
- **WHEN** a server starts with reduced artifact limits, a shorter polling interval and a smaller preview
- **THEN** actual resource and tool behavior reflects those values while default protocol contracts and execution confirmation rules remain intact

### Requirement: Configuration compatibility and secrets
Configuration migration SHALL preserve public client constructors, existing tool parameter/result contracts, device credential isolation and framework provider precedence. Documentation SHALL describe JSON, env, CLI, Docker mappings and array merging without real secrets.

#### Scenario: External source download
- **WHEN** an authorized tool downloads a source artifact from an external URL with a configured download timeout
- **THEN** it uses that timeout without sending the device NetworkPassword or changing device redirect protection

#### Scenario: Existing device URL environment variable
- **WHEN** Ultimate__BaseUrl overrides the JSON device URL
- **THEN** every server device consumer receives it through IOptions<UltimateClientOptions>.Value.BaseUrl and a malformed value prevents startup before device HTTP

## MODIFIED Requirements

### Requirement: Legacy resource subscriptions
The server SHALL isolate resource subscriptions by session, emit `notifications/resources/updated` only when a subscribed resource changes, and stop session workers when a session ends.

#### Scenario: Two independent subscribers
- **WHEN** one of two legacy sessions subscribes to a resource and its content changes
- **THEN** only that session receives an update notification for that URI

#### Scenario: Unsubscribe and cleanup
- **WHEN** the subscriber unsubscribes and later deletes its session
- **THEN** subsequent notification cycles omit the URI and its session workers stop

#### Scenario: Unchanged documentation
- **WHEN** a subscribed embedded document remains unchanged
- **THEN** polling intervals alone do not cause update notifications

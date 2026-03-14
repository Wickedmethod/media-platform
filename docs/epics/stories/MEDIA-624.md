# STORY: MEDIA-624 API Worker Contract Tests

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Add contract tests between .NET API and Node.js worker for commands and state events.

## Dependencies

- [MEDIA-620](MEDIA-620.md) versioned API contract definitions
- [MEDIA-622](MEDIA-622.md) trusted worker communication boundary

## Acceptance Criteria

- Contract tests cover command payloads and state update payloads.
- Idempotency and error contract behavior are validated.
- Contract test suite runs in CI and blocks breaking changes.

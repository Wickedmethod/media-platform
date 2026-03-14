# STORY: MEDIA-607 Idempotent Player Commands

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Make player command endpoints idempotent and state-safe under retries.

## Dependencies

- [MEDIA-003](../MEDIA-003.md) command endpoint surface
- [MEDIA-601](MEDIA-601.md) for deterministic command/state behavior

## Acceptance Criteria

- Repeating play, pause, or skip requests does not create inconsistent state.
- Responses always include authoritative current playback state.
- Duplicate command requests are handled safely.

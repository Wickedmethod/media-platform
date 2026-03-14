# STORY: MEDIA-601 Queue State Machine

## Epic

- [MEDIA-007](../MEDIA-007.md)

## Priority

- Must have

## Summary

Define a single queue and playback state machine with explicit states and transitions.

## Dependencies

- [MEDIA-203](MEDIA-203.md) for player worker integration
- [MEDIA-607](MEDIA-607.md) for idempotent command behavior

## Acceptance Criteria

- States include idle, buffering, playing, paused, and error.
- Transitions are deterministic for play, pause, skip, next, and end-of-track events.
- Invalid transitions return a clear error and do not corrupt state.

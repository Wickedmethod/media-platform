# STORY: MEDIA-618 Realtime Transport Decision

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Choose and standardize one realtime transport for v1 between WebSocket and SSE.

## Dependencies

- [MEDIA-003](../MEDIA-003.md) queue and now-playing API design
- [MEDIA-620](MEDIA-620.md) API contract versioning and event contract shape

## Acceptance Criteria

- Decision record exists with tradeoffs and final selection.
- API and worker/client contracts use one chosen transport pattern.
- Realtime reconnection and heartbeat behavior is defined.

# STORY: MEDIA-615 Multi-Room Synchronized Playback

## Epic

- [MEDIA-008](../MEDIA-008.md)

## Priority

- Advanced

## Summary

Synchronize playback across multiple Raspberry Pi players.

## Dependencies

- [MEDIA-008](../MEDIA-008.md) multi-device architecture baseline
- [MEDIA-603](MEDIA-603.md) for per-player health and readiness

## Acceptance Criteria

- Multiple players can join the same synchronized session.
- Clock drift correction keeps playback within defined sync tolerance.
- Session controls can target room or all rooms.

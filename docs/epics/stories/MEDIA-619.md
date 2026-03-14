# STORY: MEDIA-619 Redis Schema and Key Design

## Epic

- [MEDIA-007](../MEDIA-007.md)

## Priority

- Must have

## Summary

Define Redis key schema, data structures, persistence strategy, and migration rules.

## Dependencies

- [MEDIA-601](MEDIA-601.md) queue state machine model
- [MEDIA-606](MEDIA-606.md) persistence and restore requirements

## Acceptance Criteria

- Key naming conventions are documented and versioned.
- Data structures for queue, now-playing, and event data are finalized.
- Backward-compatible schema migration strategy is documented.

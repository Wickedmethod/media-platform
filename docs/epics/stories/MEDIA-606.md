# STORY: MEDIA-606 Redis Persistence and Restore Runbook

## Epic

- [MEDIA-007](../MEDIA-007.md)

## Priority

- Must have

## Summary

Enable durable Redis persistence and document restore procedures.

## Dependencies

- [MEDIA-007](../MEDIA-007.md) queue and playback state ownership
- [MEDIA-601](MEDIA-601.md) for state restore consistency

## Acceptance Criteria

- Redis persistence is configured and validated in test restore drills.
- Queue and now-playing state survive service restart.
- A runbook describes backup and restore steps.

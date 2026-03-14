# STORY: MEDIA-602 Playback Retry and Failure Handling

## Epic

- [MEDIA-002](../MEDIA-002.md)

## Priority

- Must have

## Summary

Implement retry logic and clear failure handling for playback and upstream API errors.

## Dependencies

- [MEDIA-203](MEDIA-203.md) for worker retry hooks
- [MEDIA-601](MEDIA-601.md) for valid state transitions on failures

## Acceptance Criteria

- Transient failures are retried with bounded exponential backoff.
- Permanent failures skip to next queue item and record reason.
- Failure metrics are emitted for alerting and troubleshooting.

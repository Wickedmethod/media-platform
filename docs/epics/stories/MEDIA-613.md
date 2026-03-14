# STORY: MEDIA-613 Event Notifications

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Nice to have

## Summary

Send notifications for queue events, playback failures, and state changes.

## Dependencies

- [MEDIA-003](../MEDIA-003.md) for event sources in API layer
- [MEDIA-603](MEDIA-603.md) for health/failure signal foundation

## Acceptance Criteria

- Notifications support at least one channel and one webhook endpoint.
- Critical failures trigger immediate notifications.
- Delivery failures are retried and logged.

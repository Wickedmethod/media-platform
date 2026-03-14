# STORY: MEDIA-614 Usage and Reliability Analytics

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Nice to have

## Summary

Add a basic analytics view for playback usage, failures, and command latency.

## Dependencies

- [MEDIA-613](MEDIA-613.md) for event stream and notification hooks
- [MEDIA-003](../MEDIA-003.md) for command and state telemetry inputs

## Acceptance Criteria

- Metrics include playback time, error rate, and command latency.
- Data can be filtered by time range.
- Metrics can be exported for troubleshooting.

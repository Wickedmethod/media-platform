# STORY: MEDIA-631 Security Alerts and Anomaly Detection

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Detect suspicious patterns and trigger alerts for potential account abuse.

## Dependencies

- [MEDIA-604](MEDIA-604.md) for audit events and rate limits
- [MEDIA-613](MEDIA-613.md) for notification channel integration

## Acceptance Criteria

- Alert rules exist for repeated denied requests and unusual action spikes.
- Critical security alerts are sent to at least one out-of-band channel.
- Alert payloads include enough context for investigation.

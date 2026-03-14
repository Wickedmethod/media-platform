# STORY: MEDIA-622 Worker API Trust Boundary

## Epic

- [MEDIA-002](../MEDIA-002.md)

## Priority

- Must have

## Summary

Define and implement trusted communication between player worker and API.

## Dependencies

- [MEDIA-621](MEDIA-621.md) internal network security baseline
- [MEDIA-605](MEDIA-605.md) secure secret handling patterns

## Acceptance Criteria

- Worker to API authentication mechanism is defined and implemented.
- API can distinguish worker-originated updates from client calls.
- Rejected or untrusted worker requests are audited.

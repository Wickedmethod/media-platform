# STORY: MEDIA-632 Device and Session Management

## Epic

- [MEDIA-006](../MEDIA-006.md)

## Priority

- Nice to have

## Summary

Track trusted devices and active sessions to reduce unauthorized control risk.

## Dependencies

- [MEDIA-626](MEDIA-626.md) for Keycloak-backed user authentication
- [MEDIA-621](MEDIA-621.md) for trusted network baseline

## Acceptance Criteria

- Users can view and revoke active sessions.
- Unknown device access can be restricted or challenged.
- Session revocation takes effect quickly across API and realtime channels.

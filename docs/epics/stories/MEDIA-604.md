# STORY: MEDIA-604 AuthZ Roles, Rate Limits, and Audit Logs

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Add baseline API security controls for access, abuse prevention, and traceability.

## Dependencies

- [MEDIA-401](MEDIA-401.md) for identity context and OAuth groundwork
- [MEDIA-003](../MEDIA-003.md) API endpoints for enforcement points

## Acceptance Criteria

- Keycloak roles enforce least privilege for queue and YouTube actions.
- Rate limits are applied to command endpoints.
- Security-relevant actions are written to immutable audit logs.

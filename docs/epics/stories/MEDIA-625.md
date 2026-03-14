# STORY: MEDIA-625 Primary Account OAuth Hardening

## Epic

- [MEDIA-004](../MEDIA-004.md)

## Priority

- Must have

## Summary

Allow using a primary Google account while hardening token handling and action controls.

## Dependencies

- [MEDIA-605](MEDIA-605.md) for Vault-only token storage
- [MEDIA-621](MEDIA-621.md) for internal network restrictions
- [MEDIA-626](MEDIA-626.md) for Keycloak-enforced access to YouTube actions

## Acceptance Criteria

- Google OAuth is the only login method for YouTube account consent (no password collection).
- Refresh token is stored only in Vault and never logged.
- Scope set is least-privilege and documented.
- Revoke and re-link flow is available and tested.
- Audit logs include who triggered like or playlist actions and when.

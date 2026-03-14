# STORY: MEDIA-626 Keycloak Gate for YouTube Actions

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Must have

## Summary

Use Keycloak as authorization gate for app users, while keeping Google OAuth tokens separate for YouTube API calls.

## Dependencies

- [MEDIA-401](MEDIA-401.md) for Google OAuth login and consent
- [MEDIA-604](MEDIA-604.md) for roles, rate limiting, and audit controls
- [MEDIA-620](MEDIA-620.md) for contract and error policy

## Acceptance Criteria

- App users authenticate through Keycloak before calling YouTube action endpoints.
- Role-based policies restrict who can perform like and playlist actions.
- Google OAuth tokens are not issued by Keycloak and are stored separately in Vault.
- Unauthorized requests are blocked with consistent error responses.
- Security documentation clearly separates user auth (Keycloak) from YouTube delegated access (Google OAuth).

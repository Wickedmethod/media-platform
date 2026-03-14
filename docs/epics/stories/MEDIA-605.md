# STORY: MEDIA-605 Vault-Only Secret and Token Handling

## Epic

- [MEDIA-004](../MEDIA-004.md)

## Priority

- Must have

## Summary

Ensure OAuth refresh tokens and secrets are stored and retrieved only through Vault.

## Dependencies

- [MEDIA-401](MEDIA-401.md) for OAuth token issuance
- [MEDIA-402](MEDIA-402.md) as base token storage story

## Acceptance Criteria

- No tokens are persisted in Redis or plaintext config files.
- Application retrieves secrets from Vault at runtime.
- Logs are scrubbed to prevent secret leakage.

# STORY: MEDIA-629 Vault Policy Hardening and Secret Rotation

## Epic

- [MEDIA-004](../MEDIA-004.md)

## Priority

- Must have

## Summary

Apply least-privilege Vault policies and scheduled rotation for credentials and integration secrets.

## Dependencies

- [MEDIA-605](MEDIA-605.md) for Vault-only storage model
- [MEDIA-621](MEDIA-621.md) for network controls around secret consumers

## Acceptance Criteria

- Vault policies grant minimum required read and write permissions.
- Secret rotation schedule is automated and documented.
- Rotation failure produces alert and rollback procedure.

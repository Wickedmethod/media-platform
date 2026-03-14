# STORY: MEDIA-628 Emergency Token Revoke and Kill Switch

## Epic

- [MEDIA-004](../MEDIA-004.md)

## Priority

- Must have

## Summary

Provide immediate revoke and kill-switch controls for Google token usage.

## Dependencies

- [MEDIA-625](MEDIA-625.md) for token lifecycle implementation
- [MEDIA-626](MEDIA-626.md) for access-controlled execution

## Acceptance Criteria

- Admin can revoke active tokens in one action.
- Kill switch blocks all YouTube write actions immediately.
- Revoke and kill-switch actions are audit logged.

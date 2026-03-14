# STORY: MEDIA-627 OAuth Scope Minimization and Review

## Epic

- [MEDIA-004](../MEDIA-004.md)

## Priority

- Must have

## Summary

Restrict Google OAuth scopes to least privilege and enforce periodic review.

## Dependencies

- [MEDIA-401](MEDIA-401.md) for OAuth login flow
- [MEDIA-625](MEDIA-625.md) for primary account hardening

## Acceptance Criteria

- Approved scope list is documented and implemented.
- Any new scope requires explicit review and changelog entry.
- Token requests fail if unapproved scopes are requested.

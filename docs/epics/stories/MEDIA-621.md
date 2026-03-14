# STORY: MEDIA-621 Internal Network Security Baseline

## Epic

- [MEDIA-001](../MEDIA-001.md)

## Priority

- Must have

## Summary

Apply network-level protections for v1 while authentication is deferred.

## Dependencies

- [MEDIA-604](MEDIA-604.md) future auth and authorization controls
- [MEDIA-003](../MEDIA-003.md) command endpoint inventory

## Acceptance Criteria

- API write endpoints are restricted to trusted network paths.
- Public exposure policy is documented for Caddy and ingress routing.
- Basic request logging and abuse detection are enabled.

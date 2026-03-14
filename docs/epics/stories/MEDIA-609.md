# STORY: MEDIA-609 Smart Queue Modes

## Epic

- [MEDIA-007](../MEDIA-007.md)

## Priority

- Nice to have

## Summary

Add queue modes such as shuffle, play-next insertion, and related autoplay.

## Dependencies

- [MEDIA-601](MEDIA-601.md) for queue transition rules
- [MEDIA-007](../MEDIA-007.md) for queue ownership and persistence

## Acceptance Criteria

- Queue supports normal, shuffle, and play-next insertion modes.
- Mode changes are reflected in queue ordering deterministically.
- Related autoplay can be enabled or disabled per session.

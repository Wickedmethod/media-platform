# STORY: MEDIA-612 Local Video Caching

## Epic

- [MEDIA-002](../MEDIA-002.md)

## Priority

- Nice to have

## Summary

Cache selected videos locally to reduce startup latency and internet dependency.

## Dependencies

- [MEDIA-202](MEDIA-202.md) for playback dependency setup
- [MEDIA-602](MEDIA-602.md) for cache fallback on upstream failures

## Acceptance Criteria

- Cache policy defines size limits and eviction strategy.
- Cached playback starts faster than non-cached baseline.
- Cache operations do not expose private token data.

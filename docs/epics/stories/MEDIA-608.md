# STORY: MEDIA-608 Resume Playback and Start Timestamp

## Epic

- [MEDIA-003](../MEDIA-003.md)

## Priority

- Nice to have

## Summary

Support resume-from-last-position and optional start timestamp for queued items.

## Dependencies

- [MEDIA-601](MEDIA-601.md) for resume state model
- [MEDIA-607](MEDIA-607.md) for safe replayed/resume commands

## Acceptance Criteria

- Users can set or pass a start timestamp per item.
- Playback can resume from stored last position for interrupted items.
- Timestamp validation rejects out-of-range values.

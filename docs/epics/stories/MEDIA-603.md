# STORY: MEDIA-603 Health Checks and Watchdog

## Epic

- [MEDIA-002](../MEDIA-002.md)

## Priority

- Must have

## Summary

Expose health checks and run the player worker with watchdog support.

## Dependencies

- [MEDIA-203](MEDIA-203.md) for worker process health
- [MEDIA-204](MEDIA-204.md) for systemd service wiring

## Acceptance Criteria

- Service exposes liveness and readiness signals.
- systemd watchdog restarts worker on hang or crash.
- Restart events are logged with timestamps and reason.

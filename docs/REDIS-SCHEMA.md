# Redis Schema and Key Design

## Version

Schema v1 — March 2026

## Key Naming Convention

```
media:<resource>
```

All keys are prefixed with `media:` to namespace within a shared Redis instance.

## Data Structures

### `media:queue` — List (FIFO)

Queue of pending items. Each element is a JSON-serialized `QueueItemDto`.

```json
{
  "Id": "a1b2c3d4",
  "Url": "https://www.youtube.com/watch?v=...",
  "Title": "Song Title",
  "Status": "Pending",
  "AddedAt": "2026-03-14T12:00:00Z",
  "StartAtSeconds": 0
}
```

**Operations:**

- `RPUSH` — add to end (normal mode)
- `LPUSH` — add to front (play-next mode)
- `LPOP` — dequeue next (normal mode)
- `LRANGE 0 -1` — list all items
- `LREM` — remove specific item by value
- Random index + `LREM` — shuffle dequeue

### `media:now-playing` — Hash

Current playback state.

| Field             | Type   | Description                        |
| ----------------- | ------ | ---------------------------------- |
| `state`           | string | PlayerState enum value             |
| `itemJson`        | string | JSON-serialized current QueueItem  |
| `startedAt`       | string | ISO 8601 timestamp                 |
| `positionSeconds` | string | Current playback position (double) |
| `retryCount`      | string | Current retry attempt count        |
| `lastError`       | string | Last error reason (if any)         |

### `media:queue-mode` — String

Current queue mode. Values: `Normal`, `Shuffle`, `PlayNext`.
Defaults to `Normal` when key doesn't exist.

## PlayerState Values

`Idle`, `Buffering`, `Playing`, `Paused`, `Error`, `Stopped`

## QueueItemStatus Values

`Pending`, `Playing`, `Played`, `Failed`, `Removed`

## Migration Strategy

- All keys are versioned implicitly by the schema document version.
- New fields are added with sensible defaults (missing = zero/empty).
- Backward-compatible: old data deserializes safely (see `QueueItemDto` with default `StartAtSeconds = 0`).
- Breaking schema changes require a `FLUSHALL` or a migration script documented here.

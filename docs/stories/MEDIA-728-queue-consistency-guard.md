# MEDIA-728: Queue Consistency Guard — Race Condition Protection

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** High  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** None (existing queue API)

---

## Summary

Protect the queue against race conditions when multiple users mutate state simultaneously. Two users adding/removing/skipping at the same time could corrupt queue order or cause inconsistent state. Use Redis atomic operations (Lua scripts) and optimistic concurrency to guarantee consistency.

---

## Race Condition Scenarios

| Scenario                                   | Risk                               | Solution                              |
| ------------------------------------------ | ---------------------------------- | ------------------------------------- |
| Two users skip at the same time            | Double-skip (skips 2 tracks)       | Lua script: atomic check-and-advance  |
| User adds item while admin reorders        | Item inserted at wrong position    | Queue version + optimistic lock       |
| User deletes item that's currently playing | Now-playing points to deleted item | Atomic delete-and-advance             |
| Two users add at the same time             | Duplicate position numbers         | Redis RPUSH (naturally atomic append) |
| Admin toggles kill switch during playback  | Partial state (playing + killed)   | Lua script: atomic state transition   |

---

## Queue Version Counter

Every queue mutation increments a version:

```
queue:version → 42  (Redis INCR)
```

Clients can include `X-Queue-Version: 42` on mutations. If version doesn't match, the API returns `409 Conflict`:

```csharp
app.MapDelete("/queue/{id}", async (string id, HttpContext ctx, IQueueService queue) =>
{
    var clientVersion = ctx.Request.Headers["X-Queue-Version"].FirstOrDefault();
    if (clientVersion != null)
    {
        var currentVersion = await queue.GetVersionAsync();
        if (long.Parse(clientVersion) != currentVersion)
            return Results.Conflict(new { detail = "Queue was modified. Refresh and retry." });
    }

    await queue.RemoveAsync(id);
    return Results.NoContent();
});
```

---

## Lua Scripts for Atomic Operations

### Atomic Skip (prevent double-skip)

```lua
-- skip.lua: atomically advance to next track
local currentId = redis.call('GET', KEYS[1])  -- queue:current
if currentId ~= ARGV[1] then
    return 0  -- Already skipped (idempotent)
end
local nextId = redis.call('LINDEX', KEYS[2], 0)  -- queue:items
if nextId then
    redis.call('LPOP', KEYS[2])
    redis.call('SET', KEYS[1], nextId)
    redis.call('INCR', KEYS[3])  -- queue:version
    return 1
end
return -1  -- Queue empty
```

### Atomic Delete-and-Advance

```lua
-- delete-item.lua: remove item, advance if it was playing
local removed = redis.call('LREM', KEYS[1], 1, ARGV[1])
if removed == 0 then return 0 end
redis.call('INCR', KEYS[2])  -- queue:version
local currentId = redis.call('GET', KEYS[3])  -- queue:current
if currentId == ARGV[1] then
    -- Deleted item was playing, advance to next
    local nextId = redis.call('LINDEX', KEYS[1], 0)
    if nextId then
        redis.call('SET', KEYS[3], nextId)
    else
        redis.call('DEL', KEYS[3])
    end
    return 2  -- Advanced
end
return 1  -- Removed, no advance needed
```

---

## Tasks

- [ ] Add `queue:version` Redis counter, incremented on every mutation
- [ ] Implement Lua script for atomic skip
- [ ] Implement Lua script for atomic delete-and-advance
- [ ] Support `X-Queue-Version` header for optimistic concurrency
- [ ] Return `409 Conflict` when version mismatch detected
- [ ] Unit tests for each Lua script
- [ ] Integration tests for concurrent operations (parallel test clients)

---

## Acceptance Criteria

- [ ] Two simultaneous skip requests only skip one track
- [ ] Deleting the currently playing item auto-advances to next
- [ ] Queue version increments on every mutation
- [ ] 409 Conflict returned when client sends stale version
- [ ] No data corruption under concurrent load

# MEDIA-711: API — Track "Added By" User on Queue Items

## Story

**Epic:** MEDIA-BE-MULTI — Multi-User Backend Support  
**Priority:** High  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-604 (JWT auth)

---

## Summary

Extend the queue item model to track which authenticated user added each item. The user ID and display name are extracted from the JWT token and stored alongside the queue item in Redis.

---

## Current State

```csharp
// Domain/Entities/QueueItem.cs
public record QueueItem(
    string Id,
    string Url,
    string Title,
    QueueItemStatus Status,
    DateTimeOffset AddedAt,
    int StartAtSeconds = 0);
```

Queue items have no concept of who added them.

---

## Proposed Change

```csharp
public record QueueItem(
    string Id,
    string Url,
    string Title,
    QueueItemStatus Status,
    DateTimeOffset AddedAt,
    int StartAtSeconds = 0,
    string? AddedByUserId = null,
    string? AddedByName = null);
```

### Endpoint Change (QueueEndpoints.cs)

```csharp
group.MapPost("/add", async (..., HttpContext http) =>
{
    var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var name = http.User.FindFirst("preferred_username")?.Value
            ?? http.User.FindFirst(ClaimTypes.Name)?.Value;

    var command = new AddToQueueCommand(
        request.Url, request.Title, request.StartAtSeconds,
        AddedByUserId: userId, AddedByName: name);
    // ...
});
```

### Redis Storage

The `AddedByUserId` and `AddedByName` fields are stored in the Redis hash alongside other queue item fields. The API response includes them:

```json
{
  "id": "abc123",
  "url": "https://youtube.com/watch?v=...",
  "title": "Bohemian Rhapsody",
  "status": "Pending",
  "addedAt": "2026-03-14T12:00:00Z",
  "addedByUserId": "user-uuid",
  "addedByName": "jonas"
}
```

---

## Authorization Rules

| Action | Who can do it |
|--------|--------------|
| Add to queue | Any authenticated user |
| Remove own item | The user who added it (`addedByUserId` matches) |
| Remove any item | `media-admin` role only |
| Skip current | `media-admin` role only |

---

## Tasks

- [ ] Add `AddedByUserId` and `AddedByName` to `QueueItem` record
- [ ] Update `AddToQueueCommand` to include user fields
- [ ] Extract user from JWT in `QueueEndpoints.MapPost("/add")`
- [ ] Update `RedisQueueRepository` to store/retrieve user fields
- [ ] Update `DELETE /queue/{id}` to enforce ownership (own item OR admin)
- [ ] Update API response mapper to include user fields
- [ ] Update integration tests
- [ ] Update unit tests for ownership check

---

## Acceptance Criteria

- [ ] Queue items include `addedByUserId` and `addedByName` in API response
- [ ] Anonymous (dev mode) items have null user fields
- [ ] Users can only delete their own items (unless admin)
- [ ] Attempting to delete another user's item returns 403
- [ ] Redis stores user fields correctly
- [ ] Existing tests still pass (backward compatible)

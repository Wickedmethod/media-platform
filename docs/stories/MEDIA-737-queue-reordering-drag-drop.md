# MEDIA-737: Queue Reordering — Drag & Drop

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-702 (Queue Management View), MEDIA-728 (Consistency Guard)

---

## Summary

Admin users can reorder queue items by drag-and-drop on desktop and long-press-drag on mobile. Reordering is optimistic (instant UI feedback) with server reconciliation. Requires a new backend endpoint for queue reordering.

---

## UI Interaction

### Desktop

- Grab handle (⠿) on left side of queue item
- Drag item to new position
- Drop → item snaps into place
- Other items animate to fill gaps

### Mobile

- Long-press (300ms) on queue item activates drag mode
- Haptic feedback on activation (if available)
- Drag to new position
- Drop → item snaps into place

---

## New Backend Endpoint

```
POST /queue/reorder
Authorization: Bearer <admin-jwt>
X-Queue-Version: 42

{
  "itemId": "abc-123",
  "newIndex": 2
}

Response 200: { "version": 43 }
Response 409: { "detail": "Queue was modified. Refresh and retry." }
```

### Implementation

```csharp
app.MapPost("/queue/reorder", async (ReorderRequest request, IQueueService queue) =>
{
    await queue.MoveItemAsync(request.ItemId, request.NewIndex);
    return Results.Ok(new { version = await queue.GetVersionAsync() });
})
.RequireAuthorization("AdminOnly")
.WithName("ReorderQueue")
.WithTags("Queue");
```

Uses Redis `LREM` + `LINSERT` in a Lua script for atomicity (from MEDIA-728).

---

## Frontend Implementation

```vue
<!-- Enhanced QueueList.vue -->
<script setup lang="ts">
import { useSortable } from "@vueuse/integrations/useSortable";
import { useReorderQueue } from "@/generated/api";

const el = ref<HTMLElement>();

useSortable(el, queueItems, {
  handle: ".drag-handle",
  animation: 200,
  onEnd: async (event) => {
    const { oldIndex, newIndex } = event;
    if (oldIndex === newIndex) return;

    // Optimistic update (instant)
    const item = queueItems.value.splice(oldIndex!, 1)[0];
    queueItems.value.splice(newIndex!, 0, item);

    // Server sync
    try {
      await reorderMutation.mutateAsync({
        itemId: item.id,
        newIndex: newIndex!,
      });
    } catch (err) {
      // Revert on conflict
      queryClient.invalidateQueries({ queryKey: ["queue"] });
    }
  },
});
</script>

<template>
  <div ref="el">
    <div
      v-for="item in queueItems"
      :key="item.id"
      class="flex items-center gap-3"
    >
      <span class="drag-handle cursor-grab text-muted-foreground">⠿</span>
      <QueueItem :item="item" />
    </div>
  </div>
</template>
```

---

## Dependencies

- **@vueuse/integrations** — `useSortable` (wrapper around SortableJS)
- **MEDIA-728** — Queue version for optimistic concurrency

---

## Tasks

- [ ] Create `POST /queue/reorder` endpoint (AdminOnly, version-checked)
- [ ] Implement Redis `LREM` + `LINSERT` Lua script for atomic reorder
- [ ] Add `useSortable` to `QueueList.vue` (desktop drag)
- [ ] Add long-press handler for mobile drag activation
- [ ] Implement optimistic update with server reconciliation
- [ ] Revert on 409 Conflict (refresh queue)
- [ ] Add drag handle (⠿) to `QueueItem.vue` (admin only)
- [ ] Integration test for reorder endpoint

---

## Acceptance Criteria

- [ ] Admin can drag queue items to reorder on desktop
- [ ] Admin can long-press + drag on mobile
- [ ] Reorder is optimistic (instant visual feedback)
- [ ] 409 Conflict reverts UI to server state
- [ ] Non-admin users don't see drag handles
- [ ] SSE `queue-updated` event fires after reorder

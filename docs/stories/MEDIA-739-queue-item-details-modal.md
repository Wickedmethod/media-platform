# MEDIA-739: Queue Item Details Modal

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-702 (Queue Management View), MEDIA-711 (Added-by tracking)

---

## Summary

Tapping a queue item opens a details modal showing full metadata: title, URL, duration, who added it, when it was added, and playback history. Admins get additional actions (delete, play next, move to top).

---

## UI Design

```
┌──────────────────────────────┐
│  ✕        Queue Item Details │
├──────────────────────────────┤
│                              │
│  ┌────────────────────────┐  │
│  │  ░░░░░░░░░░░░░░░░░░░░ │  │  ← YouTube thumbnail
│  │  ░░░░ Thumbnail ░░░░░ │  │
│  └────────────────────────┘  │
│                              │
│  Bohemian Rhapsody           │
│  Queen — Official Video      │
│                              │
│  🔗 youtube.com/watch?v=...  │
│  ⏱ 5:55 duration             │
│  👤 Added by @jonas           │
│  🕐 Added 12 min ago         │
│  📊 Position: #3 in queue    │
│                              │
│  ┌──────────┐ ┌──────────┐  │
│  │ ▶ Play   │ │ 🗑 Remove │  │  ← Admin only
│  │   Next   │ │          │  │
│  └──────────┘ └──────────┘  │
│  ┌──────────────────────┐   │
│  │ ⬆ Move to Top        │   │  ← Admin only
│  └──────────────────────┘   │
│                              │
└──────────────────────────────┘
```

---

## Component

```vue
<!-- src/features/queue/QueueItemModal.vue -->
<script setup lang="ts">
import { Dialog, DialogContent, DialogHeader } from '@/shared/components/ui/dialog'

const props = defineProps<{ item: QueueItem; isAdmin: boolean }>()
const emit = defineEmits<{ close: [] }>()

const thumbnailUrl = computed(() =>
  `https://img.youtube.com/vi/${extractVideoId(props.item.url)}/mqdefault.jpg`
)

const addedAgo = computed(() => formatRelativeTime(props.item.addedAt))
</script>

<template>
  <Dialog :open="true" @update:open="emit('close')">
    <DialogContent class="max-w-md">
      <DialogHeader>Queue Item Details</DialogHeader>

      <img :src="thumbnailUrl" class="w-full rounded-lg" />
      <h3 class="text-lg font-semibold">{{ item.title }}</h3>

      <div class="space-y-2 text-sm text-muted-foreground">
        <p>🔗 {{ item.url }}</p>
        <p>⏱ {{ formatDuration(item.duration) }}</p>
        <p>👤 Added by {{ item.addedByName ?? 'Unknown' }}</p>
        <p>🕐 {{ addedAgo }}</p>
      </div>

      <div v-if="isAdmin" class="flex gap-2 mt-4">
        <Button @click="playNext(item.id)">▶ Play Next</Button>
        <Button variant="destructive" @click="removeItem(item.id)">🗑 Remove</Button>
      </div>
    </DialogContent>
  </Dialog>
</template>
```

---

## Tasks

- [ ] Create `QueueItemModal.vue` using shadcn Dialog
- [ ] Show YouTube thumbnail from video ID
- [ ] Display metadata: title, URL, duration, added-by, added-at
- [ ] Add admin actions: Remove, Play Next, Move to Top
- [ ] Wire modal open/close from `QueueItem.vue` tap
- [ ] Add relative time formatting (e.g., "12 min ago")

---

## Acceptance Criteria

- [ ] Tapping queue item opens modal with full details
- [ ] YouTube thumbnail displayed
- [ ] Added-by name and relative time shown
- [ ] Admin sees action buttons (Remove, Play Next)
- [ ] Non-admin sees read-only details
- [ ] Modal closes on ✕ or outside click

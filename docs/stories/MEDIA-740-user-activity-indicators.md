# MEDIA-740: User Activity Indicators — "Added by" Display

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-702 (Queue Management View), MEDIA-711 (Added-by tracking), MEDIA-704 (SSE composable)

---

## Summary

Show real-time user activity in the SPA: who added each song (avatar + name on queue items), and **real-time toast notifications** when someone adds a song. Users see "🎵 @jonas added Bohemian Rhapsody" the moment it happens via SSE.

---

## Queue Item Display

Each queue item shows who added it:

```
┌────────────────────────────────────────┐
│  🎵 Bohemian Rhapsody — Queen          │
│  👤 jonas · 12 min ago           [🗑]  │
└────────────────────────────────────────┘
```

For TV guest additions:

```
┌────────────────────────────────────────┐
│  🎵 Gangnam Style — PSY               │
│  📺 TV · 3 min ago                     │
└────────────────────────────────────────┘
```

---

## Real-Time Song-Added Toast

When any user (or TV) adds a song, all connected SPA clients see a toast:

```
┌──────────────────────────────┐
│ 🎵 @jonas added a song       │
│ Bohemian Rhapsody — Queen    │
└──────────────────────────────┘
```

This is powered by the `item-added` SSE event (see MEDIA-712, MEDIA-704):

```typescript
// Already wired in usePlayerStore (MEDIA-704):
case 'item-added':
  useToast().show({
    type: 'info',
    title: `${event.data.addedByName} added a song`,
    message: event.data.title,
    duration: 4000,
  })
  queryClient.invalidateQueries({ queryKey: ['queue'] })
  break
```

---

## User Identity Sources

| Source                  | Display                    | Icon |
| ----------------------- | -------------------------- | ---- |
| Keycloak JWT (SPA user) | `preferred_username` claim | 👤   |
| X-Worker-Key (TV guest) | "TV"                       | 📺   |
| Unknown / legacy        | "Unknown"                  | ❓   |

---

## Components

### UserBadge.vue

```vue
<script setup lang="ts">
const props = defineProps<{
  userId: string | null;
  userName: string | null;
}>();

const isTV = computed(() => props.userId === "tv-guest");
const icon = computed(() => (isTV.value ? "📺" : "👤"));
const displayName = computed(() => props.userName ?? "Unknown");
</script>

<template>
  <span class="inline-flex items-center gap-1 text-sm text-muted-foreground">
    <span>{{ icon }}</span>
    <span>{{ displayName }}</span>
  </span>
</template>
```

---

## Tasks

- [ ] Create `UserBadge.vue` component
- [ ] Add `UserBadge` to `QueueItem.vue` with added-by data
- [ ] Add relative timestamp ("12 min ago") next to user badge
- [ ] Verify `item-added` SSE toast is working (MEDIA-704)
- [ ] Handle "tv-guest" identity with TV icon
- [ ] Handle null/missing user data gracefully

---

## Acceptance Criteria

- [ ] Each queue item shows who added it with name and icon
- [ ] TV additions show "📺 TV" identity
- [ ] Toast notification appears when another user adds a song
- [ ] Toast shows user name and song title
- [ ] Relative timestamps update periodically

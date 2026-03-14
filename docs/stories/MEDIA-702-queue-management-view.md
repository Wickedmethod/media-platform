# MEDIA-702: Queue Management View

## Story

**Epic:** MEDIA-FE-ADMIN — Admin & User Frontend  
**Priority:** High  
**Effort:** 5 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700, MEDIA-701

---

## Summary

The primary view of the app. Users can see the current queue, add videos, and see who added what. Admin users can reorder, remove any item, and change queue mode.

---

## UI Design

### Mobile Layout (primary)

```
┌──────────────────────────┐
│ ♪ media::platform   [☰]  │  ← Nav header
├──────────────────────────┤
│  ▶ Now Playing            │
│  Rick Astley - Never...   │
│  Added by: @jonas         │
│  ▶ 2:34 / 3:32           │
│  [⏸] [⏭] [⏹]            │  ← Only if admin
├──────────────────────────┤
│  🔎 Search YouTube...     │  ← Inline search
├──────────────────────────┤
│  Queue (3 items)  [Mode▾] │
│  ┌────────────────────┐   │
│  │ 1. Gangnam Style   │   │
│  │    @sarah · 🗑      │   │  ← 🗑 only for own items (or admin)
│  ├────────────────────┤   │
│  │ 2. Despacito       │   │
│  │    @jonas · 🗑      │   │
│  ├────────────────────┤   │
│  │ 3. Bohemian Rhaps  │   │
│  │    @maria           │   │
│  └────────────────────┘   │
├──────────────────────────┤
│  [+] Add URL manually     │
└──────────────────────────┘
```

### Desktop Layout

Same content, but wider cards + now-playing as a persistent sidebar or top bar.

---

## Features

### All Users

- View current queue with "added by" user names
- Add video by pasting YouTube URL (with title auto-detection)
- Add video via YouTube search (see MEDIA-710)
- Remove own items from queue
- See now-playing state in real-time (SSE)

### Admin Only

- Remove ANY item from queue
- Reorder queue items (drag & drop on desktop, long-press + move on mobile)
- Change queue mode (Normal / Shuffle / PlayNext)
- Player controls (play, pause, skip, stop)

---

## Components

| Component | Purpose |
|-----------|---------|
| `QueueView.vue` | Main page layout |
| `NowPlaying.vue` | Current track card with progress |
| `QueueList.vue` | Scrollable queue with items |
| `QueueItem.vue` | Single queue item (title, user, actions) |
| `AddToQueue.vue` | URL input + optional title |
| `QueueModeSelector.vue` | Normal/Shuffle/PlayNext toggle |
| `PlayerControls.vue` | Play/Pause/Skip/Stop bar (admin only) |

---

## Real-time Updates

The queue updates in real-time via SSE:

```typescript
// composables/useSSE.ts
const eventSource = new EventSource('/events')
eventSource.addEventListener('queue-updated', () => queryClient.invalidateQueries(['queue']))
eventSource.addEventListener('playback-state', (e) => playerStore.updateState(JSON.parse(e.data)))
```

No polling needed. SSE handles all updates.

---

## API Endpoints Used

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/queue` | List queue items |
| POST | `/queue/add` | Add item |
| DELETE | `/queue/{id}` | Remove item |
| GET | `/queue/mode` | Get current mode |
| POST | `/queue/mode` | Set mode (admin) |
| GET | `/now-playing` | Current playback state |
| POST | `/player/play` | Play (admin) |
| POST | `/player/pause` | Pause (admin) |
| POST | `/player/skip` | Skip (admin) |
| POST | `/player/stop` | Stop (admin) |
| GET | `/events` | SSE stream |

---

## Tasks

- [ ] Create `QueueView.vue` as main route (`/`)
- [ ] Create `NowPlaying.vue` with state badge, title, progress bar, "added by"
- [ ] Create `QueueList.vue` with `QueueItem.vue` children
- [ ] Create `AddToQueue.vue` with URL input + title
- [ ] Create `PlayerControls.vue` (shown only for admin role)
- [ ] Create `QueueModeSelector.vue` (shown only for admin role)
- [ ] Wire SSE events to TanStack Query invalidation
- [ ] Add swipe-to-delete on mobile for own items
- [ ] Add role-based visibility (v-if="isAdmin")
- [ ] Responsive layout: mobile-first card stack

---

## Acceptance Criteria

- [ ] Queue items display with title, URL, and "added by" username
- [ ] Users can add items and remove their own items
- [ ] Admins see player controls and can remove any item
- [ ] Queue updates in real-time via SSE (no manual refresh)
- [ ] Now-playing card shows current state with live position
- [ ] Works well on mobile (iPhone SE width minimum)
- [ ] Queue mode selector works (admin only)

---

## Notes

- "Added by" requires the API to track which user added each item (see MEDIA-711)
- Queue reorder (drag & drop) is a stretch goal for v1
- YouTube title auto-detection is a nice-to-have (can be done client-side via oembed)

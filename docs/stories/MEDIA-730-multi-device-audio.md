# MEDIA-730: Multi-Device Audio — Separate Playback Sessions

## Story

**Epic:** MEDIA-MULTI — Multi-Device Audio  
**Priority:** High  
**Effort:** 8 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-711 (user tracking), MEDIA-701 (auth)

---

## Summary

Enable multiple users to listen to different songs on different devices simultaneously. Each authenticated user can have their own playback session with a personal queue, while the TV continues playing the shared/party queue. This is the "personal headphones" mode — users browse the shared queue for inspiration but play their own selections on their phone/laptop.

---

## Concept

```
Shared Queue (TV)          User Queues (Personal)
┌────────────────┐         ┌────────────────┐
│ Party playlist │         │ Jonas's Queue  │
│ Song A ▶       │         │ Song X ▶       │ ← playing on Jonas's phone
│ Song B         │         │ Song Y         │
│ Song C         │         │ Song Z         │
└────────────────┘         └────────────────┘
        │                          │
        ▼                  ┌────────────────┐
   TV (YouTube              │ Maria's Queue  │
   IFrame player)           │ Song P ▶       │ ← playing on Maria's laptop
                            │ Song Q         │
                            └────────────────┘
```

### Key Principle

The **TV** is the party speaker — it plays the shared queue controlled by everyone.  
**Personal devices** are headphone mode — each user plays their own queue on their own device.

---

## Architecture Changes

### 1. Session Model

```csharp
// Domain/Entities/PlaybackSession.cs
public record PlaybackSession(
    string SessionId,          // "shared" for TV, or user-specific UUID
    string? UserId,            // null for shared session
    string? DeviceId,          // Client-generated device identifier
    SessionType Type,          // Shared | Personal
    List<QueueItem> Queue,
    int CurrentIndex,
    PlayerState State,
    DateTimeOffset CreatedAt);

public enum SessionType { Shared, Personal }
```

### 2. API Changes

New endpoints under `/sessions/`:

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/sessions/personal` | Create a personal session for the authenticated user |
| `GET` | `/sessions/mine` | Get the user's active personal session |
| `POST` | `/sessions/{id}/queue/add` | Add to a specific session's queue |
| `POST` | `/sessions/{id}/player/play` | Play on a specific session |
| `POST` | `/sessions/{id}/player/skip` | Skip on a specific session |
| `POST` | `/sessions/{id}/player/pause` | Pause a specific session |
| `GET` | `/sessions/{id}/events` | SSE stream for a specific session |
| `DELETE` | `/sessions/{id}` | End a personal session |

The existing `/queue/*` and `/player/*` endpoints continue to work — they operate on the **shared** session (backward compatible).

### 3. Redis Storage

```
media:session:shared          → shared session state (existing)
media:session:{userId}:{deviceId} → personal session state
media:sessions:active         → SET of active session IDs
```

### 4. Frontend: Personal Player

The Vue SPA gets an embedded audio/video player for personal sessions:

```vue
<!-- src/features/personal-player/PersonalPlayer.vue -->
<template>
  <div v-if="personalSession" class="personal-player">
    <!-- Hidden YouTube IFrame for audio -->
    <div id="personal-yt-player" class="hidden" />

    <div class="personal-controls">
      <Button @click="togglePlay" variant="ghost" size="icon">
        <Play v-if="!isPlaying" /> <Pause v-else />
      </Button>
      <div class="flex-1">
        <p class="text-sm font-medium truncate">{{ currentTrack.title }}</p>
        <Progress :value="progress" class="h-1" />
      </div>
      <Button @click="skip" variant="ghost" size="icon">
        <SkipForward />
      </Button>
    </div>
  </div>
</template>
```

### 5. Queue View Changes

The queue view gets a tab switcher:

```
┌─────────────────────────────────┐
│  [🎵 Party Queue] [🎧 My Queue] │
├─────────────────────────────────┤
│                                 │
│  (queue items for selected tab) │
│                                 │
└─────────────────────────────────┘
```

- **Party Queue**: The shared queue (everyone sees same items, TV plays these)
- **My Queue**: Personal queue (only this user, plays on their device)
- Users can "copy" a song from Party Queue to My Queue

---

## User Flow

### Starting Personal Listening

1. User taps "🎧 My Queue" tab
2. If no personal session: "Start personal session" button shown
3. User taps → `POST /sessions/personal` creates session
4. User searches and adds songs to personal queue
5. YouTube IFrame (hidden, audio only) plays on their device
6. Personal player mini-bar appears at bottom

### Ending Personal Listening

1. User taps "End session" in personal player
2. `DELETE /sessions/{id}` cleans up
3. Returns to Party Queue view

---

## YouTube Audio on Mobile

YouTube IFrame API works on mobile but has restrictions:

- **Autoplay blocked**: First play requires a user gesture (tap)
- **Background play**: Requires the tab to stay active (not minimize)
- **Audio only**: IFrame can be 1×1px or hidden, audio still plays

```javascript
// Personal YouTube player (hidden, audio only)
const personalPlayer = new YT.Player('personal-yt-player', {
  height: '1',
  width: '1',
  playerVars: {
    autoplay: 0,       // User gesture required first time
    controls: 0,
    playsinline: 1,    // Don't go fullscreen on mobile
  },
  events: {
    onStateChange: (e) => {
      if (e.data === YT.PlayerState.ENDED) {
        playNextInPersonalQueue()
      }
    },
  },
})
```

---

## Limitations & Considerations

| Concern | Decision |
|---------|----------|
| YouTube ToS | IFrame API is officially supported; hidden player is a gray area but widely used |
| Mobile background | Tab must stay active; PWA helps but iOS Safari pauses background audio |
| Bandwidth | Each personal session = separate YouTube stream; 5 users = 5 streams |
| Complexity | This is the most complex feature; consider phasing (v1 = shared only, v2 = personal) |

---

## Phased Implementation

### Phase 1 (v1): Shared Only ← Current
All users share one queue, TV is the only playback device.

### Phase 2 (v2): Personal Sessions ← This Story
Users can create personal sessions with their own queues and on-device playback.

### Phase 3 (v3, future): Multi-Room
Multiple TVs, each with their own shared session. Rooms with independent queues.

---

## Tasks

### Backend
- [ ] Create `PlaybackSession` entity
- [ ] Create `SessionRepository` (Redis)
- [ ] Add `POST /sessions/personal` endpoint
- [ ] Add `GET /sessions/mine` endpoint
- [ ] Add session-scoped queue endpoints
- [ ] Add session-scoped player endpoints
- [ ] Add per-session SSE streams
- [ ] Ensure shared session backward compatibility
- [ ] Add session cleanup (expire after 4h idle)
- [ ] Update authorization (users can only control their own sessions)

### Frontend
- [ ] Create `usePersonalSession` composable
- [ ] Create `PersonalPlayer.vue` with hidden YouTube IFrame
- [ ] Add Party/Personal queue tab switcher
- [ ] Create "Start personal session" flow
- [ ] Implement "Copy to My Queue" action from Party Queue
- [ ] Handle YouTube autoplay restriction (user gesture)
- [ ] Create personal player mini-bar
- [ ] Handle session expiry gracefully

---

## Acceptance Criteria

- [ ] User can create a personal playback session
- [ ] Personal queue is independent from shared queue
- [ ] Songs play on the user's device (YouTube audio)
- [ ] TV continues playing shared queue unaffected
- [ ] Multiple users can have simultaneous personal sessions
- [ ] "Copy to My Queue" works from shared queue
- [ ] Session auto-cleans after 4h idle
- [ ] Existing `/queue/*` and `/player/*` still work (shared session)
- [ ] Personal session requires Keycloak authentication

---

## Notes

- This is a complex feature that spans both backend and frontend
- Consider implementing as v2 after the basic frontend is working
- YouTube background audio on iOS is problematic — may need "keep screen on" workaround
- Alternative to YouTube IFrame: use youtube-dl/yt-dlp to extract audio URLs (more complex, potential ToS issues)

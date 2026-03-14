# MEDIA-755: Player Playback Timeout Detection

## Story

**Epic:** MEDIA-PI-OPS — Player Operations & Diagnostics  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-720 (TV player), MEDIA-724 (heartbeat)

---

## Summary

Detect when a YouTube video fails to start playing within a timeout window (buffering stall, video unavailable, network issue). If playback doesn't begin within 30 seconds of loading, auto-skip to the next track and report the failure to the API.

**Related:** MEDIA-724 (heartbeat) detects dead players. MEDIA-736 (error screen) handles YouTube error codes. This story handles the gap: video loads but never plays (no error event, just silence).

---

## Detection Logic

```
loadVideoById() called
    │
    ├── YT.PlayerState.PLAYING within 30s → ✅ OK, reset timer
    │
    └── 30s elapsed, still BUFFERING/UNSTARTED → ⚠️ Timeout
        │
        ├── Retry once (reload same video)
        │   ├── PLAYING within 15s → ✅ OK
        │   └── 15s elapsed → ❌ Skip to next
        │
        └── Report to API: POST /player/error
            { "type": "playback_timeout", "videoId": "...", "phase": "initial|retry" }
```

---

## Implementation

### TV Player Integration

```typescript
// src/features/tv/composables/usePlaybackTimeout.ts
import { ref, watch, onUnmounted } from 'vue'

interface TimeoutOptions {
  initialTimeoutMs?: number   // default: 30000
  retryTimeoutMs?: number     // default: 15000
  onTimeout: (videoId: string, phase: 'initial' | 'retry') => void
  onSkip: (videoId: string) => void
}

export function usePlaybackTimeout(options: TimeoutOptions) {
  const {
    initialTimeoutMs = 30_000,
    retryTimeoutMs = 15_000,
    onTimeout,
    onSkip,
  } = options

  let timer: ReturnType<typeof setTimeout> | null = null
  let currentVideoId: string | null = null
  let retryAttempted = false

  function startWatching(videoId: string) {
    clearTimer()
    currentVideoId = videoId
    retryAttempted = false

    timer = setTimeout(() => {
      onTimeout(videoId, 'initial')
      retryAttempted = true
      // Caller should reload video — start retry timer
      timer = setTimeout(() => {
        onSkip(videoId)
      }, retryTimeoutMs)
    }, initialTimeoutMs)
  }

  function playbackStarted() {
    clearTimer()
  }

  function clearTimer() {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  onUnmounted(clearTimer)

  return { startWatching, playbackStarted }
}
```

### Wiring into TvPlayer

```typescript
// Inside TvPlayer.vue setup
const timeout = usePlaybackTimeout({
  onTimeout: (videoId, phase) => {
    if (phase === 'initial') {
      // Retry: reload same video
      ytPlayer.loadVideoById(videoId)
    }
    reportError('playback_timeout', videoId, phase)
  },
  onSkip: (videoId) => {
    reportError('playback_timeout_skip', videoId, 'retry')
    playerApi.skip() // Move to next track
  },
})

// When new track loads
watch(() => playerStore.currentItem, (item) => {
  if (item) timeout.startWatching(extractVideoId(item.url))
})

// When YouTube reports PLAYING state
function onPlayerStateChange(e: YT.OnStateChangeEvent) {
  if (e.data === YT.PlayerState.PLAYING) {
    timeout.playbackStarted()
  }
}
```

---

## API Error Reporting

Extend the existing `POST /player/error` to accept timeout errors:

```json
{
  "type": "playback_timeout",
  "videoId": "dQw4w9WgXcQ",
  "phase": "initial",
  "details": "Video did not start within 30s"
}
```

---

## Timeout Flow Summary

| Phase | Duration | Outcome |
|-------|----------|---------|
| Initial load | 30s | If PLAYING → OK. If not → retry |
| Retry | 15s | If PLAYING → OK. If not → skip |
| After skip | — | Next track loads, new timeout starts |

---

## Tasks

- [ ] Create `usePlaybackTimeout` composable
- [ ] Wire into `TvPlayer.vue` (start on track change, cancel on PLAYING)
- [ ] Implement retry logic (reload same video once)
- [ ] Call `POST /player/error` with `playback_timeout` type
- [ ] Auto-skip after retry failure
- [ ] Show timeout indicator on TV (TvError.vue integration)
- [ ] Unit tests for timeout composable (start, cancel, retry, skip)

---

## Acceptance Criteria

- [ ] Video not playing within 30s triggers retry
- [ ] Retry failure within 15s triggers auto-skip
- [ ] Error reported to API with timeout type and phase
- [ ] Normal videos (start < 30s) are unaffected
- [ ] Timer resets on each new track

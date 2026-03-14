# MEDIA-720: TV Frontend — Pi Kiosk Application

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** High  
**Effort:** 5 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-704 (SSE composable, pattern reference)

---

## Summary

Build a dedicated TV frontend optimized for Raspberry Pi 4/5 running in Chromium kiosk mode. The TV is the **second entry point** in the shared Vue project (`tv.html` → `src/tv.ts` → `TvApp.vue`). It reuses shared composables (`useSSE`, Orval API client, `usePlayerStore`) but has its own lightweight component tree — no router, no Keycloak, no admin features.

**Absorbs:** MEDIA-735 (TV Idle Screen & Queue Preview)

---

## Architecture

```
Pi 4/5 (Chromium kiosk)
    │
    ├── TV Frontend (this story) — Vue 3 (shared project, separate entry)
    │   ├── TvApp.vue (root)
    │   │   ├── TvPlayer.vue (YouTube IFrame)
    │   │   ├── TvOverlay.vue (now-playing bar)
    │   │   ├── TvIdle.vue (idle screen + queue preview)
    │   │   ├── TvSearch.vue (on-screen keyboard, MEDIA-722)
    │   │   └── TvError.vue (error screen, MEDIA-736)
    │   ├── useSSE composable (shared from SPA)
    │   ├── usePlayerStore (shared from SPA)
    │   └── Orval API client (shared from SPA)
    │
    └── CEC listener (MEDIA-721)
        └── WebSocket → useCEC composable → local navigation
```

### Why Vue (not vanilla HTML/JS)?

- **Shared composables** — `useSSE`, `usePlayerStore`, API client reused without duplication
- **Reactive state** — player state, overlay visibility, search results all reactive
- **Component-based** — TvPlayer, TvOverlay, TvIdle, TvSearch, TvError are clean components
- **Tree-shaking** — TV entry imports only what it needs; no Keycloak, no Router, no admin code
- **Type safety** — shared TypeScript types for SSE events, API responses
- **Still lightweight** — no router, no state management lib beyond reactive refs, fast boot

---

## Screen States

### 1. Idle State (nothing playing) — absorbed from MEDIA-735

```
┌──────────────────────────────────────────┐
│                                          │
│           🎵 Media Platform              │
│                                          │
│         Waiting for music...             │
│                                          │
│     Next up:                             │
│     1. Bohemian Rhapsody — Queen         │
│     2. Gangnam Style — PSY              │
│     3. Despacito — Luis Fonsi           │
│                                          │
│     Add songs from your phone            │
│     or press OK to search.              │
│                                          │
│                          14:32  Wi-Fi ●  │
└──────────────────────────────────────────┘
```

- Subtle animated gradient background (CSS only)
- **Queue preview** — shows next 3 items if queue is not empty
- Clock in corner
- Hint text about adding songs or pressing OK to search
- Transitions to playing state when SSE `track-changed` event arrives

### 2. Playing State

```
┌──────────────────────────────────────────┐
│                                          │
│                                          │
│          ┌────────────────────┐          │
│          │                    │          │
│          │   YouTube Player   │          │
│          │   (fullscreen)     │          │
│          │                    │          │
│          │                    │          │
│          └────────────────────┘          │
│                                          │
│ ▶ Bohemian Rhapsody    ██████░░  3:42   │  ← Overlay (auto-hide)
└──────────────────────────────────────────┘
```

- YouTube IFrame fills entire screen
- Overlay bar appears on track change, then fades after 5s
- Overlay toggled with OK button on remote (MEDIA-721)

### 3. Search State (MEDIA-722)

```
┌──────────────────────────────────────────┐
│  🔍 Search YouTube                       │
│  ┌──────────────────────────────────┐    │
│  │ bohemian rha█                    │    │
│  └──────────────────────────────────┘    │
│                                          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │ Result 1 │ │ Result 2│ │ Result 3│   │
│  │ ▶ 3:42  │ │ ▶ 4:01  │ │ ▶ 5:23  │   │
│  └─────────┘ └─────────┘ └─────────┘   │
│                                          │
│  [Q][W][E][R][T][Y][U][I][O][P]         │
│  [A][S][D][F][G][H][J][K][L]            │
│  [⬆][Z][X][C][V][B][N][M][⌫]           │
│  [___________SPACE___________][Search]   │
└──────────────────────────────────────────┘
```

(Detailed in MEDIA-722 — TV On-Screen Keyboard & Search)

---

## YouTube IFrame Player — Vue Component

```vue
<!-- src/features/tv/TvPlayer.vue -->
<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { usePlayerStore } from '@/stores/player'

const playerStore = usePlayerStore()
const playerRef = ref<HTMLDivElement>()
let ytPlayer: YT.Player | null = null

function initPlayer() {
  ytPlayer = new YT.Player(playerRef.value!, {
    height: '100%',
    width: '100%',
    playerVars: {
      autoplay: 1,
      controls: 0,
      modestbranding: 1,
      rel: 0,
      showinfo: 0,
      iv_load_policy: 3,
      fs: 0,
    },
    events: {
      onStateChange: onPlayerStateChange,
      onError: onPlayerError,
    },
  })
}

function onPlayerStateChange(e: YT.OnStateChangeEvent) {
  if (e.data === YT.PlayerState.ENDED) {
    reportTrackEnd()
  }
}

function onPlayerError(e: YT.OnErrorEvent) {
  reportPlaybackError(e.data)
}

// Watch for track changes from SSE
watch(() => playerStore.currentItem, (item) => {
  if (item && ytPlayer) {
    ytPlayer.loadVideoById({
      videoId: extractVideoId(item.url),
      startSeconds: playerStore.position,
    })
  }
})
</script>

<template>
  <div ref="playerRef" class="absolute inset-0 bg-black" />
</template>
```

  onPlayerStateChange(event) {
    if (event.data === YT.PlayerState.ENDED) {
      // Report to API — triggers next in queue
      fetch('/player/report-end', { method: 'POST' })
    }
  }

  // Position reporting every 5s
  startPositionReporting() {
    setInterval(() => {
      if (this.player?.getPlayerState() === YT.PlayerState.PLAYING) {
        const pos = Math.floor(this.player.getCurrentTime())
        fetch('/player/position', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ positionSeconds: pos }),
        })
      }
    }, 5000)
  }
}
```

---

## SSE Connection

```javascript
class TVEventSource {
  constructor(apiBase) {
    this.apiBase = apiBase
    this.connect()
  }

  connect() {
    this.es = new EventSource(`${this.apiBase}/events`)

    this.es.onmessage = (event) => {
      const data = JSON.parse(event.data)
      this.handleEvent(data)
    }

    this.es.onerror = () => {
      this.es.close()
      setTimeout(() => this.connect(), 3000)
    }
  }

  handleEvent(event) {
    switch (event.type) {
      case 'track-changed':
        tvPlayer.play(event.data.videoId, event.data.startAt)
        break
      case 'state-changed':
        if (event.data.state === 'Paused') tvPlayer.player.pauseVideo()
        if (event.data.state === 'Playing') tvPlayer.player.playVideo()
        if (event.data.state === 'Stopped') tvPlayer.player.stopVideo()
        break
      case 'kill-switch':
        if (event.data.active) {
          tvPlayer.player.stopVideo()
          showKillSwitchOverlay()
        }
        break
    }
  }
}
```

---

## Overlay Bar

The overlay shows:

```
┌──────────────────────────────────────────┐
│ ▶  Song Title                ██░░  3:42  │
│    Added by: jonas            Queue: 4   │
└──────────────────────────────────────────┘
```

- **Auto-show** on track change for 5 seconds
- **Toggle** with OK button on CEC remote
- **Progress bar**: thin accent-colored bar at bottom of overlay
- **Fade in/out** animation (CSS transitions)
- Semi-transparent background (`rgba(10, 10, 15, 0.85)`)
- Positioned at bottom of screen

---

## Pi Kiosk Setup

The Pi boots directly into Chromium kiosk:

```bash
# /etc/xdg/lxsession/LXDE-pi/autostart
@chromium-browser --kiosk --noerrdialogs --disable-infobars \
  --disable-translate --no-first-run --fast --fast-start \
  --disable-features=TranslateUI --disk-cache-size=50000000 \
  http://media-platform-api:8080/tv.html
```

### Performance Optimizations

- **GPU acceleration**: `--enable-gpu-rasterization --enable-zero-copy`
- **No scroll bars**: CSS `overflow: hidden` on body
- **No cursor**: `cursor: none` on body
- **Disable screen blanking**: `xset s off && xset -dpms`
- **Memory limit**: Chromium `--max-old-space-size=256`

---

## File Structure

```
src/MediaPlatform.Api/wwwroot/
├── tv.html           ← Main TV page (self-contained)
├── tv.css            ← TV styles (extracted for caching)
└── tv.js             ← TV logic (extracted for caching)
```

Alternatively, served from a separate nginx container (see MEDIA-706).

---

## Tasks

- [ ] Rewrite `tv.html` as proper fullscreen kiosk application
- [ ] Implement YouTube IFrame player controller
- [ ] Implement SSE event handler for TV
- [ ] Create overlay bar with auto-show/hide
- [ ] Create idle state screen (no queue)
- [ ] Add position reporting to API (every 5s)
- [ ] Add error handling (video unavailable, API unreachable)
- [ ] Add CSS-only animated gradient for idle state
- [ ] Optimize for Pi Chromium (GPU, memory, no-cursor)
- [ ] Test on Pi 4 with 1080p TV
- [ ] Test auto-recovery after API restart
- [ ] Test video transitions (end → next track)

---

## Acceptance Criteria

- [ ] TV shows idle screen when queue is empty
- [ ] YouTube video plays fullscreen when track starts
- [ ] Overlay bar appears on track change, auto-hides after 5s
- [ ] SSE drives all state changes (no polling)
- [ ] Position reporting works (API receives updates)
- [ ] Reconnects automatically if API goes down
- [ ] Kill switch shows "Playback Disabled" overlay
- [ ] No visible cursor, scrollbars, or browser UI
- [ ] Page loads in < 3s on Pi 4
- [ ] Stable for 24+ hours without memory leaks

---

## Notes

- This replaces the existing `tv.html` prototype
- The TV frontend does NOT require Keycloak auth — it's a trusted device on the local network
- The X-Worker-Key header is used for API calls that need authorization (position reporting, report-end)

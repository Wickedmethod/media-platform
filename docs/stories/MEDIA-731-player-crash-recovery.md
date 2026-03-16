# MEDIA-731: Player Crash Recovery & Auto-Reconnect

## Story

**Epic:** MEDIA-002 — Raspberry Pi Player Node  
**Priority:** High  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-720 (TV frontend), MEDIA-724 (heartbeat), MEDIA-729 (registration)

---

## Summary

When the Pi player crashes and restarts (Chromium crash, power loss, network drop), it should automatically recover to the correct state: re-register, reconnect SSE, fetch the latest state via `/sync`, and resume playback at the correct position. Zero manual intervention required.

---

## Recovery Flow

```
Player crashes / power cycles
    │
    ▼ systemd restarts Chromium kiosk
    │
    ▼ TV Vue app loads
    │
    1. POST /worker/register  (re-announce)
    2. GET /sync              (fetch full state)
    3. Connect EventSource /events
    4. Compare local vs server state
    │
    ├── Server has active track → resume playback at server position
    ├── Server is idle → show idle screen
    └── Server is paused → show paused overlay
```

---

## Local State Persistence

The TV app persists minimal state to `localStorage` for crash recovery:

```typescript
interface PersistedState {
  lastVideoId: string | null;
  lastPosition: number;
  lastState: PlayerState;
  timestamp: number; // when this was saved
}

// Save every 10s while playing
function persistState() {
  const state: PersistedState = {
    lastVideoId: playerStore.currentItem?.videoId ?? null,
    lastPosition: playerStore.position,
    lastState: playerStore.playerState,
    timestamp: Date.now(),
  };
  localStorage.setItem("tv-state", JSON.stringify(state));
}
```

---

## Reconciliation Logic

On startup, compare local persisted state with server state from `/sync`:

```typescript
async function reconcile() {
  const local = loadPersistedState();
  const server = await fetch("/sync").then((r) => r.json());

  if (!server.nowPlaying.currentItem) {
    // Server is idle — show idle screen
    return showIdle();
  }

  if (server.nowPlaying.state === "Playing") {
    // Resume playback at server's position
    return playVideo(
      server.nowPlaying.currentItem.videoId,
      server.nowPlaying.position,
    );
  }

  if (server.nowPlaying.state === "Paused") {
    // Load video but don't auto-play
    return loadVideo(
      server.nowPlaying.currentItem.videoId,
      server.nowPlaying.position,
    );
  }
}
```

---

## SSE Reconnect with Exponential Backoff

```typescript
const MAX_RETRIES = 10;
const BASE_DELAY = 1000; // 1s
const MAX_DELAY = 30000; // 30s

function connectSSE(retryCount = 0) {
  const source = new EventSource("/events");

  source.onopen = () => {
    retryCount = 0; // Reset on success
  };

  source.onerror = () => {
    source.close();
    const delay = Math.min(BASE_DELAY * 2 ** retryCount, MAX_DELAY);
    setTimeout(() => connectSSE(retryCount + 1), delay);
  };
}
```

After `MAX_RETRIES`, fall back to polling `/sync` every 10s.

---

## Crash Detection Signals

| Signal           | How Detected         | Recovery Action        |
| ---------------- | -------------------- | ---------------------- |
| Chromium crash   | systemd restarts it  | Full recovery flow     |
| Network drop     | SSE `onerror`        | Reconnect + `/sync`    |
| API restart      | SSE connection drops | Reconnect + `/sync`    |
| Power loss       | Pi reboots           | Full recovery flow     |
| JavaScript error | `window.onerror`     | Log + attempt recovery |

---

## Tasks

- [ ] Implement `localStorage` state persistence (save every 10s)
- [ ] Implement reconciliation logic on TV app mount
- [ ] Implement SSE reconnect with exponential backoff
- [ ] Add polling fallback after max SSE retries
- [ ] Add `window.onerror` handler for JS crash recovery
- [ ] Verify systemd restart policy in provisioning (MEDIA-723)
- [ ] Manual test: kill Chromium process → verify auto-recovery

---

## Acceptance Criteria

- [ ] After Chromium crash, TV resumes playback within 10s
- [ ] After network drop, SSE reconnects with backoff
- [ ] Player position is within 15s of actual after recovery
- [ ] Idle screen shown if server has no active track
- [ ] No manual intervention required for any crash scenario

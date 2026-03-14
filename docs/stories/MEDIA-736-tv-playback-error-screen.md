# MEDIA-736: TV Playback Error Screen

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-720 (TV frontend)

---

## Summary

When a video fails to play on the TV (geo-blocked, removed, embedding disabled, network error), show a clear error screen with the reason and automatic recovery: retry once, then auto-skip to the next track after 10 seconds.

---

## Error Sources

| Error              | YouTube Error Code | User Message                       |
| ------------------ | ------------------ | ---------------------------------- |
| Video not found    | 100                | "Video not found or removed"       |
| Embedding disabled | 101, 150           | "This video can't be played on TV" |
| Playback error     | 2, 5               | "Playback error — skipping..."     |
| Network timeout    | (none)             | "Network error — retrying..."      |
| API unreachable    | (none)             | "Can't reach server — retrying..." |

---

## UI Design

```
┌──────────────────────────────────────────┐
│                                          │
│                                          │
│           ⚠️ Can't Play Video            │
│                                          │
│     "Bohemian Rhapsody — Queen"          │
│                                          │
│     This video can't be played on TV.    │
│     Embedding is disabled by uploader.   │
│                                          │
│     Skipping in 8s...  [████░░░░░░]     │
│                                          │
│     Press → to skip now                  │
│                                          │
│                          14:32  Wi-Fi ●  │
└──────────────────────────────────────────┘
```

---

## Recovery Logic

```typescript
// src/features/tv/TvError.vue
const RETRY_DELAY = 3_000; // Retry once after 3s
const SKIP_DELAY = 10_000; // Auto-skip after 10s

async function handlePlaybackError(error: PlaybackError) {
  // 1. Report error to API
  await reportError(error);

  // 2. Retry once for transient errors (network, timeout)
  if (isRetryable(error) && retryCount < 1) {
    retryCount++;
    setTimeout(() => retryPlayback(), RETRY_DELAY);
    return;
  }

  // 3. Show error screen with countdown
  showErrorScreen(error);
  startSkipCountdown(SKIP_DELAY);
}

function startSkipCountdown(duration: number) {
  const start = Date.now();
  const timer = setInterval(() => {
    const elapsed = Date.now() - start;
    progress.value = elapsed / duration;
    remaining.value = Math.ceil((duration - elapsed) / 1000);
    if (elapsed >= duration) {
      clearInterval(timer);
      skipToNext();
    }
  }, 100);
}

function isRetryable(error: PlaybackError): boolean {
  return error.code === undefined || error.code === 2 || error.code === 5;
}
```

---

## API Error Reporting

The TV reports playback errors so the API can track problematic videos:

```
POST /player/error
X-Worker-Key: <key>

{
  "videoId": "dQw4w9WgXcQ",
  "error": "Embedding disabled",
  "errorCode": 150,
  "queueItemId": "abc-123"
}
```

This triggers the existing retry logic (MEDIA-602) on the server side.

---

## Tasks

- [ ] Create `TvError.vue` component with error message and countdown
- [ ] Map YouTube error codes to user-friendly messages
- [ ] Implement auto-retry for transient errors (1 attempt)
- [ ] Implement auto-skip countdown (10s with progress bar)
- [ ] Wire CEC `→` button to immediate skip from error screen
- [ ] Report errors to API via `POST /player/error`
- [ ] Transition back to playing state when skip succeeds

---

## Acceptance Criteria

- [ ] Error screen shows video title and human-readable error
- [ ] Transient errors retry once before showing error screen
- [ ] Auto-skip countdown visible with progress bar
- [ ] CEC right arrow skips immediately from error screen
- [ ] After skip, playback resumes with next track

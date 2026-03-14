# MEDIA-710: YouTube Search Integration

## Story

**Epic:** MEDIA-FE-ADMIN — Admin & User Frontend  
**Priority:** High  
**Effort:** 5 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700, MEDIA-701

---

## Summary

Users can search YouTube directly from the frontend (and from the TV interface). Search results show video titles, thumbnails, channel names, and duration. Users tap to add to queue.

---

## Architecture

### Option A: Client-Side Search (v1 — no API key needed)

Use YouTube's oembed + Invidious API (public, no key):

```
Frontend → https://vid.puffyan.us/api/v1/search?q=query
         → Returns titles, thumbnails, videoIds
         → User selects → POST /queue/add with URL
```

### Option B: Backend Proxy (v2 — requires Google API key)

```
Frontend → GET /youtube/search?q=query
API → YouTube Data API v3
    → Returns structured results
```

**Decision: Start with Option A** (no API key dependency). Move to Option B when Google OAuth stories are done.

---

## UI Design

### Mobile Search

```
┌──────────────────────────┐
│  🔎 bohemian rhapsody     │  ← Search input with debounce
├──────────────────────────┤
│  ┌────────────────────┐  │
│  │ [thumb] Bohemian   │  │
│  │         Rhapsody   │  │
│  │  Queen · 5:55  [+] │  │
│  ├────────────────────┤  │
│  │ [thumb] Bohemian   │  │
│  │         Rhapsody   │  │
│  │  Live · 6:12   [+] │  │
│  └────────────────────┘  │
└──────────────────────────┘
```

### TV Search (CEC navigable)

```
┌─────────────────────────────────────┐
│                                     │
│   🔎 ████████████████████            │  ← On-screen keyboard
│                                     │
│   [Q][W][E][R][T][Y][U][I][O][P]   │
│   [A][S][D][F][G][H][J][K][L]      │
│   [Z][X][C][V][B][N][M][⌫][⏎]     │
│                                     │
│   Results:                          │
│   ▸ Bohemian Rhapsody - Queen       │
│     Bohemian Rhapsody Live          │
│     Bohemian Rhapsody Piano         │
│                                     │
└─────────────────────────────────────┘
```

---

## Features

- **Debounced search** — 300ms debounce on keystroke
- **Result cards** — Thumbnail, title, channel, duration
- **Quick add** — Tap [+] to add directly to queue
- **Recent searches** — Stored locally for quick re-search
- **TV keyboard** — On-screen keyboard navigable with CEC (up/down/left/right/OK)

---

## Components

### Frontend (Vue)

| Component            | Purpose                |
| -------------------- | ---------------------- |
| `YouTubeSearch.vue`  | Search input + results |
| `SearchResult.vue`   | Single result card     |
| `RecentSearches.vue` | Recent search terms    |

### TV (HTML)

| Element            | Purpose                                      |
| ------------------ | -------------------------------------------- |
| On-screen keyboard | Letter-by-letter input via CEC               |
| Results list       | CEC-navigable (up/down to select, OK to add) |

---

## Tasks

- [ ] Create search composable `useYouTubeSearch.ts` with Invidious API
- [ ] Create `YouTubeSearch.vue` with debounced input
- [ ] Create `SearchResult.vue` with thumbnail, title, channel, duration
- [ ] Add [+] button that calls `POST /queue/add`
- [ ] Store recent searches in localStorage
- [ ] Create TV search UI with on-screen keyboard
- [ ] Make TV results navigable with CEC arrows
- [ ] Handle Invidious API fallback (multiple instances)
- [ ] Add loading skeleton for results

---

## Acceptance Criteria

- [ ] User can search YouTube by typing in search box
- [ ] Results show within 500ms of typing (debounced)
- [ ] Results display thumbnail, title, channel, duration
- [ ] Tapping [+] adds video to queue with correct URL and title
- [ ] TV search works with CEC-navigable on-screen keyboard
- [ ] Graceful fallback if Invidious is unreachable
- [ ] Recent searches persist across sessions

---

## Notes

- Invidious instances: `vid.puffyan.us`, `invidious.snopyta.org`, `inv.tux.pizza`
- Consider caching search results in TanStack Query (5min stale time)
- TV on-screen keyboard is a stretch goal — can start with phone-only search
- Duration comes from Invidious response (`lengthSeconds` field)

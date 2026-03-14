# MEDIA-716: Invidious Instance Management & Search Resilience

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-710 (YouTube search)

---

## Summary

YouTube search (MEDIA-710 and MEDIA-722) depends on the Invidious API — a third-party service with no SLA. This story adds resilience: multiple Invidious instances with automatic failover, health checking, and graceful degradation when all instances are unavailable.

---

## Problem

```
MEDIA-710 / MEDIA-722
    │
    ▼
Single Invidious instance (vid.puffyan.us)
    │
    ✕ Down → Search completely broken
```

Public Invidious instances go down regularly — they're community-run with rate limits. Relying on one instance is a single point of failure.

---

## Solution: Instance Pool with Failover

```typescript
// src/lib/invidious.ts
const INVIDIOUS_INSTANCES = [
  "https://vid.puffyan.us",
  "https://invidious.nerdvpn.de",
  "https://inv.nadeko.net",
  "https://invidious.privacyredirect.com",
  "https://invidious.protokolla.fi",
];

interface InstanceState {
  url: string;
  healthy: boolean;
  lastCheck: number;
  failCount: number;
}

class InvidiousClient {
  private instances: InstanceState[];
  private currentIndex = 0;

  constructor(urls: string[]) {
    this.instances = urls.map((url) => ({
      url,
      healthy: true,
      lastCheck: 0,
      failCount: 0,
    }));
  }

  /** Get next healthy instance (round-robin) */
  private getNextInstance(): InstanceState | null {
    const now = Date.now();
    for (let i = 0; i < this.instances.length; i++) {
      const idx = (this.currentIndex + i) % this.instances.length;
      const inst = this.instances[idx];

      // Skip unhealthy instances unless 60s cooldown passed
      if (!inst.healthy && now - inst.lastCheck < 60_000) continue;

      this.currentIndex = (idx + 1) % this.instances.length;
      return inst;
    }
    return null; // All instances down
  }

  /** Search with automatic failover */
  async search(query: string, maxRetries = 3): Promise<SearchResult[]> {
    for (let attempt = 0; attempt < maxRetries; attempt++) {
      const instance = this.getNextInstance();
      if (!instance) break;

      try {
        const url = `${instance.url}/api/v1/search?q=${encodeURIComponent(query)}&type=video`;
        const response = await fetch(url, {
          signal: AbortSignal.timeout(5000), // 5s timeout per instance
        });

        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const data = await response.json();
        instance.healthy = true;
        instance.failCount = 0;
        return data;
      } catch {
        instance.failCount++;
        instance.lastCheck = Date.now();
        if (instance.failCount >= 3) {
          instance.healthy = false;
        }
        // Try next instance
      }
    }

    throw new InvidiousUnavailableError(
      "All Invidious instances are unavailable",
    );
  }
}

export const invidiousClient = new InvidiousClient(INVIDIOUS_INSTANCES);
```

---

## Instance List Management

The instance list can be:

1. **Hardcoded** (simplest, current approach) — update in code when instances change
2. **Fetched from API** — `GET /config/invidious-instances` returns the list from server config
3. **Auto-discovered** — fetch from `https://api.invidious.io/instances.json` (official instance list)

For v1, hardcode 5 reliable instances. The list can be updated easily since it's a constant in one file.

---

## Graceful Degradation UI

When all instances are down:

```vue
<!-- In SearchView.vue -->
<div v-if="searchError" class="text-center py-8">
  <ServerCrash class="h-10 w-10 text-muted-foreground mx-auto mb-3" />
  <h3 class="font-medium">Search unavailable</h3>
  <p class="text-sm text-muted-foreground mt-1">
    YouTube search is temporarily unavailable. You can still add songs by URL.
  </p>
  <div class="mt-4 flex gap-2 justify-center">
    <Button variant="outline" @click="retrySearch">Try Again</Button>
    <Button @click="showAddByUrl = true">Add by URL</Button>
  </div>
</div>
```

The "Add by URL" fallback always works — it just needs a YouTube URL pasted directly.

---

## Health Check (Background)

```typescript
// Check instance health every 5 minutes
setInterval(() => {
  invidiousClient.healthCheck()
}, 300_000)

// Health check: fetch /api/v1/stats from each instance
async healthCheck() {
  for (const inst of this.instances) {
    try {
      const res = await fetch(`${inst.url}/api/v1/stats`, {
        signal: AbortSignal.timeout(3000),
      })
      inst.healthy = res.ok
      inst.failCount = 0
    } catch {
      inst.healthy = false
    }
    inst.lastCheck = Date.now()
  }
}
```

---

## Tasks

- [ ] Create `InvidiousClient` class with instance pool
- [ ] Implement round-robin instance selection
- [ ] Implement automatic failover on request failure
- [ ] Add 5s timeout per request and 60s cooldown for failed instances
- [ ] Create "Search unavailable" fallback UI with "Add by URL" option
- [ ] Add background health check (every 5 minutes)
- [ ] Extract instance list to a config constant
- [ ] Write unit tests for failover logic
- [ ] Write unit tests for health check

---

## Acceptance Criteria

- [ ] Search works even if first Invidious instance is down
- [ ] Failover to next instance happens automatically (< 5s delay)
- [ ] Failed instances are skipped for 60s before retrying
- [ ] If all instances are down, user sees "Search unavailable" with "Add by URL" option
- [ ] Background health check restores instances after they come back
- [ ] No hardcoded single instance anywhere in the codebase

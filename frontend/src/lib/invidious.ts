export interface InvidiousVideo {
  videoId: string;
  title: string;
  author: string;
  authorId: string;
  lengthSeconds: number;
  videoThumbnails: {
    quality: string;
    url: string;
    width: number;
    height: number;
  }[];
}

export interface SearchResult {
  videoId: string;
  title: string;
  channel: string;
  duration: number;
  thumbnailUrl: string;
  youtubeUrl: string;
}

const INVIDIOUS_INSTANCES = [
  "https://vid.puffyan.us",
  "https://inv.nadeko.net",
  "https://invidious.privacyredirect.com",
  "https://invidious.protokolla.fi",
  "https://invidious.nerdvpn.de",
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
  private healthCheckTimer: ReturnType<typeof setInterval> | null = null;

  constructor(urls: string[]) {
    this.instances = urls.map((url) => ({
      url,
      healthy: true,
      lastCheck: 0,
      failCount: 0,
    }));
  }

  /** Get next healthy instance (round-robin), re-try unhealthy after 60s cooldown */
  private getNextInstance(): InstanceState | null {
    const now = Date.now();
    for (let i = 0; i < this.instances.length; i++) {
      const idx = (this.currentIndex + i) % this.instances.length;
      const inst = this.instances[idx]!;
      if (!inst.healthy && now - inst.lastCheck < 60_000) continue;
      this.currentIndex = (idx + 1) % this.instances.length;
      return inst;
    }
    return null;
  }

  async search(
    query: string,
    signal?: AbortSignal,
  ): Promise<SearchResult[]> {
    const maxRetries = Math.min(this.instances.length, 5);
    let lastError: Error | null = null;

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      const instance = this.getNextInstance();
      if (!instance) break;

      try {
        const url = new URL("/api/v1/search", instance.url);
        url.searchParams.set("q", query);
        url.searchParams.set("type", "video");
        url.searchParams.set("sort_by", "relevance");

        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 5000);

        // Forward external signal
        if (signal) {
          signal.addEventListener("abort", () => controller.abort(), {
            once: true,
          });
        }

        try {
          const response = await fetch(url.toString(), {
            signal: controller.signal,
          });
          clearTimeout(timeout);

          if (!response.ok) throw new Error(`HTTP ${response.status}`);

          const data = (await response.json()) as InvidiousVideo[];
          instance.healthy = true;
          instance.failCount = 0;

          return data
            .filter((item) => item.videoId && item.title)
            .map((item) => ({
              videoId: item.videoId,
              title: item.title,
              channel: item.author,
              duration: item.lengthSeconds,
              thumbnailUrl: getBestThumbnail(item.videoThumbnails),
              youtubeUrl: `https://www.youtube.com/watch?v=${item.videoId}`,
            }));
        } catch (err) {
          clearTimeout(timeout);
          throw err;
        }
      } catch (err) {
        if (signal?.aborted) throw err;
        instance.failCount++;
        instance.lastCheck = Date.now();
        if (instance.failCount >= 3) {
          instance.healthy = false;
        }
        lastError = err instanceof Error ? err : new Error(String(err));
      }
    }

    throw lastError ?? new Error("All Invidious instances are unavailable");
  }

  async healthCheck(): Promise<void> {
    for (const inst of this.instances) {
      try {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 3000);
        const res = await fetch(`${inst.url}/api/v1/stats`, {
          signal: controller.signal,
        });
        clearTimeout(timeout);
        inst.healthy = res.ok;
        inst.failCount = 0;
      } catch {
        inst.healthy = false;
      }
      inst.lastCheck = Date.now();
    }
  }

  startHealthChecks(intervalMs = 300_000): void {
    this.stopHealthChecks();
    this.healthCheckTimer = setInterval(() => this.healthCheck(), intervalMs);
  }

  stopHealthChecks(): void {
    if (this.healthCheckTimer) {
      clearInterval(this.healthCheckTimer);
      this.healthCheckTimer = null;
    }
  }
}

function getBestThumbnail(
  thumbnails: InvidiousVideo["videoThumbnails"],
): string {
  const preferred = thumbnails.find(
    (t) => t.quality === "medium" || t.quality === "default",
  );
  return preferred?.url ?? thumbnails[0]?.url ?? "";
}

export const invidiousClient = new InvidiousClient(INVIDIOUS_INSTANCES);

// Start background health checks
invidiousClient.startHealthChecks();

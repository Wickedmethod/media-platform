import { ref, watch, readonly } from "vue";
import { useQuery } from "@tanstack/vue-query";

export interface InvidiousVideo {
  videoId: string;
  title: string;
  author: string;
  authorId: string;
  lengthSeconds: number;
  videoThumbnails: { quality: string; url: string; width: number; height: number }[];
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
  "https://inv.tux.pizza",
  "https://invidious.nerdvpn.de",
];

const RECENT_SEARCHES_KEY = "media-platform:recent-searches";
const MAX_RECENT_SEARCHES = 10;

function loadRecentSearches(): string[] {
  try {
    const stored = localStorage.getItem(RECENT_SEARCHES_KEY);
    return stored ? (JSON.parse(stored) as string[]) : [];
  } catch {
    return [];
  }
}

function saveRecentSearches(searches: string[]) {
  localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(searches));
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

function getBestThumbnail(
  thumbnails: InvidiousVideo["videoThumbnails"],
): string {
  const preferred = thumbnails.find(
    (t) => t.quality === "medium" || t.quality === "default",
  );
  return preferred?.url ?? thumbnails[0]?.url ?? "";
}

async function searchInvidious(
  query: string,
  signal?: AbortSignal,
): Promise<SearchResult[]> {
  let lastError: Error | null = null;

  for (const instance of INVIDIOUS_INSTANCES) {
    try {
      const url = new URL("/api/v1/search", instance);
      url.searchParams.set("q", query);
      url.searchParams.set("type", "video");
      url.searchParams.set("sort_by", "relevance");

      const response = await fetch(url.toString(), { signal });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = (await response.json()) as InvidiousVideo[];
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
      if (signal?.aborted) throw err;
      lastError = err instanceof Error ? err : new Error(String(err));
    }
  }

  throw lastError ?? new Error("All Invidious instances unreachable");
}

export function useYouTubeSearch() {
  const query = ref("");
  const debouncedQuery = ref("");
  const recentSearches = ref<string[]>(loadRecentSearches());
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  // Debounce search input
  watch(query, (val) => {
    if (debounceTimer) clearTimeout(debounceTimer);
    const trimmed = val.trim();
    if (!trimmed) {
      debouncedQuery.value = "";
      return;
    }
    debounceTimer = setTimeout(() => {
      debouncedQuery.value = trimmed;
    }, 300);
  });

  const {
    data: results,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["youtube-search", debouncedQuery],
    queryFn: ({ signal }) => searchInvidious(debouncedQuery.value, signal),
    enabled: () => debouncedQuery.value.length > 0,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });

  function addToRecent(term: string) {
    const trimmed = term.trim();
    if (!trimmed) return;
    const filtered = recentSearches.value.filter((s) => s !== trimmed);
    recentSearches.value = [trimmed, ...filtered].slice(0, MAX_RECENT_SEARCHES);
    saveRecentSearches(recentSearches.value);
  }

  function removeRecent(term: string) {
    recentSearches.value = recentSearches.value.filter((s) => s !== term);
    saveRecentSearches(recentSearches.value);
  }

  function clearRecent() {
    recentSearches.value = [];
    saveRecentSearches([]);
  }

  function selectRecent(term: string) {
    query.value = term;
  }

  return {
    query,
    results,
    isLoading,
    isError,
    error,
    recentSearches: readonly(recentSearches),
    addToRecent,
    removeRecent,
    clearRecent,
    selectRecent,
    formatDuration,
  };
}

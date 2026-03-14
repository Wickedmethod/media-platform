import { ref, watch, readonly } from "vue";
import { useQuery, useQueryClient } from "@tanstack/vue-query";
import { config } from "@/config";
import type { SearchResult } from "@/lib/invidious";

export type { SearchResult } from "@/lib/invidious";

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

export function useYouTubeSearch() {
  const queryClient = useQueryClient();
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
    queryFn: async ({ signal }): Promise<SearchResult[]> => {
      const url = `${config.apiBaseUrl}/search/youtube?q=${encodeURIComponent(debouncedQuery.value)}`;
      const res = await fetch(url, { signal });
      if (!res.ok) throw new Error(`Search failed (${res.status})`);
      return res.json() as Promise<SearchResult[]>;
    },
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

  function retrySearch() {
    queryClient.invalidateQueries({ queryKey: ["youtube-search"] });
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
    retrySearch,
    formatDuration,
  };
}

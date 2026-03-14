<script setup lang="ts">
import { ref } from "vue";
import { Search, AlertCircle, Loader2 } from "lucide-vue-next";
import { useQueryClient } from "@tanstack/vue-query";
import {
  useYouTubeSearch,
  type SearchResult as SearchResultType,
} from "@/composables/useYouTubeSearch";
import { useAddToQueue, getGetQueueQueryKey } from "@/generated/queue/queue";
import { useToast } from "@/composables/useToast";
import SearchResultCard from "./SearchResult.vue";
import RecentSearches from "./RecentSearches.vue";

const queryClient = useQueryClient();
const toast = useToast();
const {
  query,
  results,
  isLoading,
  isError,
  recentSearches,
  addToRecent,
  removeRecent,
  clearRecent,
  selectRecent,
} = useYouTubeSearch();

const addingId = ref<string | null>(null);

const addMutation = useAddToQueue({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
      toast.success("Added to queue");
      addingId.value = null;
    },
    onError: () => {
      toast.error("Failed to add to queue");
      addingId.value = null;
    },
  },
});

function handleAdd(result: SearchResultType) {
  addingId.value = result.videoId;
  addToRecent(query.value);
  addMutation.mutate({
    data: { url: result.youtubeUrl, title: result.title },
  });
}

function handleSelectRecent(term: string) {
  selectRecent(term);
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <!-- Search input -->
    <div class="relative">
      <Search
        class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
      />
      <input
        v-model="query"
        type="text"
        placeholder="Search YouTube..."
        class="w-full rounded-lg border border-input bg-background py-2.5 pl-10 pr-4 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
      />
    </div>

    <!-- Recent searches (shown when no query) -->
    <RecentSearches
      v-if="!query.trim()"
      :searches="recentSearches"
      @select="handleSelectRecent"
      @remove="removeRecent"
      @clear="clearRecent"
    />

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
    </div>

    <!-- Error -->
    <div
      v-else-if="isError"
      class="flex flex-col items-center gap-2 py-12 text-center"
    >
      <AlertCircle class="h-8 w-8 text-destructive" />
      <p class="text-sm text-muted-foreground">
        Search unavailable — Invidious instances may be down
      </p>
    </div>

    <!-- Results -->
    <div v-else-if="results && results.length > 0" class="space-y-1">
      <p class="text-xs text-muted-foreground">
        {{ results.length }} results
      </p>
      <SearchResultCard
        v-for="result in results"
        :key="result.videoId"
        :result="result"
        :is-adding="addingId === result.videoId"
        @add="handleAdd"
      />
    </div>

    <!-- No results -->
    <div
      v-else-if="query.trim() && !isLoading"
      class="py-12 text-center text-sm text-muted-foreground"
    >
      No results found for "{{ query.trim() }}"
    </div>
  </div>
</template>

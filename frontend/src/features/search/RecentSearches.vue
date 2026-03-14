<script setup lang="ts">
import { Clock, X } from "lucide-vue-next";

const props = defineProps<{
  searches: readonly string[];
}>();

const emit = defineEmits<{
  select: [term: string];
  remove: [term: string];
  clear: [];
}>();
</script>

<template>
  <div v-if="props.searches.length > 0">
    <div class="mb-2 flex items-center justify-between">
      <h3 class="text-xs font-semibold uppercase text-muted-foreground">
        Recent searches
      </h3>
      <button
        class="text-xs text-muted-foreground hover:text-foreground"
        @click="emit('clear')"
      >
        Clear all
      </button>
    </div>
    <div class="flex flex-wrap gap-2">
      <button
        v-for="term in props.searches"
        :key="term"
        class="group flex items-center gap-1.5 rounded-full bg-muted px-3 py-1.5 text-sm hover:bg-accent"
        @click="emit('select', term)"
      >
        <Clock class="h-3 w-3 text-muted-foreground" />
        <span>{{ term }}</span>
        <X
          class="h-3 w-3 text-muted-foreground opacity-0 hover:text-foreground group-hover:opacity-100"
          @click.stop="emit('remove', term)"
        />
      </button>
    </div>
  </div>
</template>

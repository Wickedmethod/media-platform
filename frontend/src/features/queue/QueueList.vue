<script setup lang="ts">
import type { QueueItemResponse } from "@/generated/models";
import QueueItem from "./QueueItem.vue";
import EmptyState from "@/shared/components/EmptyState.vue";
import QueueItemSkeleton from "@/shared/components/QueueItemSkeleton.vue";
import { ListMusic } from "lucide-vue-next";

defineProps<{
  items: QueueItemResponse[];
  isLoading: boolean;
}>();

const emit = defineEmits<{
  remove: [id: string];
}>();
</script>

<template>
  <div class="space-y-2">
    <!-- Loading -->
    <template v-if="isLoading">
      <QueueItemSkeleton v-for="i in 4" :key="i" />
    </template>

    <!-- Empty -->
    <EmptyState
      v-else-if="items.length === 0"
      :icon="ListMusic"
      title="Queue is empty"
      description="Add a video to get started"
    />

    <!-- Items -->
    <template v-else>
      <QueueItem
        v-for="(item, idx) in items"
        :key="item.id"
        :item="item"
        :index="idx"
        @remove="emit('remove', $event)"
      />
    </template>
  </div>
</template>

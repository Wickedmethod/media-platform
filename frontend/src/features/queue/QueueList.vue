<script setup lang="ts">
import { ref } from "vue";
import type { QueueItemResponse } from "@/generated/models";
import QueueItem from "./QueueItem.vue";
import EmptyState from "@/shared/components/EmptyState.vue";
import QueueItemSkeleton from "@/shared/components/QueueItemSkeleton.vue";
import { ListMusic } from "lucide-vue-next";
import { useAuthStore } from "@/stores/auth";

const props = defineProps<{
  items: QueueItemResponse[];
  isLoading: boolean;
  showCopyAction?: boolean;
}>();

const emit = defineEmits<{
  remove: [id: string];
  select: [item: QueueItemResponse];
  reorder: [itemId: string, newIndex: number];
  copy: [item: QueueItemResponse];
}>();

const auth = useAuthStore();

// Drag-and-drop state
const dragIndex = ref<number | null>(null);

function onDragStart(idx: number) {
  dragIndex.value = idx;
}

function onDragOver(e: DragEvent) {
  e.preventDefault();
}

function onDrop(targetIdx: number) {
  if (dragIndex.value === null || dragIndex.value === targetIdx) {
    dragIndex.value = null;
    return;
  }
  const item = props.items[dragIndex.value];
  if (item) {
    emit("reorder", item.id, targetIdx);
  }
  dragIndex.value = null;
}

function onDragEnd() {
  dragIndex.value = null;
}
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
      <div
        v-for="(item, idx) in items"
        :key="item.id"
        :draggable="auth.isAdmin"
        @dragstart="onDragStart(idx)"
        @dragover="onDragOver"
        @drop="onDrop(idx)"
        @dragend="onDragEnd"
      >
        <QueueItem
          :item="item"
          :index="idx"
          :draggable="auth.isAdmin"
          :show-copy-action="showCopyAction"
          @remove="emit('remove', $event)"
          @select="emit('select', $event)"
          @copy="emit('copy', $event)"
        />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { Button } from "@/shared/components/ui/button";
import type { QueueMode } from "@/stores/player";

const props = defineProps<{
  currentMode: string;
}>();

const emit = defineEmits<{
  change: [mode: QueueMode];
}>();

const modes: { value: QueueMode; label: string }[] = [
  { value: "Sequential", label: "Normal" },
  { value: "Shuffle", label: "Shuffle" },
  { value: "RepeatOne", label: "Repeat 1" },
  { value: "RepeatAll", label: "Repeat All" },
];
</script>

<template>
  <div class="flex gap-1">
    <Button
      v-for="mode in modes"
      :key="mode.value"
      :variant="props.currentMode === mode.value ? 'default' : 'outline'"
      size="sm"
      class="text-xs"
      @click="emit('change', mode.value)"
    >
      {{ mode.label }}
    </Button>
  </div>
</template>

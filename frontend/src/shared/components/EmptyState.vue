<script setup lang="ts">
import type { Component } from "vue";

defineProps<{
  icon: Component;
  title: string;
  description: string;
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center py-16 text-center">
    <component :is="icon" class="h-12 w-12 text-muted-foreground mb-4" />
    <h3 class="text-lg font-medium">{{ title }}</h3>
    <p class="text-sm text-muted-foreground mt-1 max-w-xs">{{ description }}</p>
    <slot name="action" />
  </div>
</template>

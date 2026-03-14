<script setup lang="ts">
import { Skeleton } from "@/shared/components/ui/skeleton";
</script>

<template>
  <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
    <div v-for="i in 4" :key="i" class="rounded-lg border p-4 space-y-3">
      <Skeleton class="h-4 w-1/2" />
      <Skeleton class="h-8 w-2/3" />
      <Skeleton class="h-3 w-full" />
    </div>
  </div>
</template>

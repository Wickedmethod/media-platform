<script setup lang="ts">
import { Skeleton } from "@/shared/components/ui/skeleton";
</script>

<template>
  <div class="flex items-center gap-3 p-3 rounded-lg">
    <Skeleton class="h-12 w-12 rounded" />
    <div class="flex-1 space-y-2">
      <Skeleton class="h-4 w-3/4" />
      <Skeleton class="h-3 w-1/3" />
    </div>
    <Skeleton class="h-8 w-8 rounded-full" />
  </div>
</template>

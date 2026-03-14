<script setup lang="ts">
import { Skeleton } from '@/shared/components/ui/skeleton'
</script>

<template>
  <div class="flex items-center gap-3 p-4">
    <Skeleton class="h-10 w-10 rounded-full" />
    <div class="flex-1 space-y-2">
      <Skeleton class="h-4 w-2/3" />
      <Skeleton class="h-1 w-full rounded" />
    </div>
    <Skeleton class="h-3 w-12" />
  </div>
</template>

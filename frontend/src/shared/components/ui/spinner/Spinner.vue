<script setup lang="ts">
import { Loader2 } from 'lucide-vue-next'

withDefaults(defineProps<{ size?: 'sm' | 'md' | 'lg' }>(), { size: 'md' })

const sizeMap = { sm: 'h-4 w-4', md: 'h-6 w-6', lg: 'h-10 w-10' } as const
</script>

<template>
  <div class="flex items-center justify-center p-4">
    <Loader2 :class="['animate-spin text-muted-foreground', sizeMap[size]]" />
  </div>
</template>

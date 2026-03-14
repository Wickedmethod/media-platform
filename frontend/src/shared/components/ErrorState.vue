<script setup lang="ts">
import { AlertCircle } from 'lucide-vue-next'
import { Button } from '@/shared/components/ui/button'

defineProps<{ error: Error }>()
defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="flex flex-col items-center justify-center py-12 text-center">
    <AlertCircle class="h-10 w-10 text-destructive mb-3" />
    <h3 class="text-lg font-medium">Failed to load</h3>
    <p class="text-sm text-muted-foreground mt-1 max-w-xs">{{ error.message }}</p>
    <Button variant="outline" size="sm" class="mt-4" @click="$emit('retry')">
      Try Again
    </Button>
  </div>
</template>

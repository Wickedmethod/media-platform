<script setup lang="ts">
import { AlertCircle } from "lucide-vue-next";
import { Button } from "@/shared/components/ui/button";

defineProps<{ error: Error }>();
defineEmits<{ retry: [] }>();
</script>

<template>
  <div
    class="flex flex-col items-center justify-center min-h-[200px] gap-4 p-8 text-center"
  >
    <AlertCircle class="h-12 w-12 text-destructive" />
    <h2 class="text-lg font-semibold">Something went wrong</h2>
    <p class="text-sm text-muted-foreground max-w-md">{{ error.message }}</p>
    <Button variant="outline" @click="$emit('retry')">Try Again</Button>
  </div>
</template>

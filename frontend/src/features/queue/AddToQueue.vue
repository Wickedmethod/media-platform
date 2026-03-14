<script setup lang="ts">
import { ref } from "vue";
import { Plus, Loader2 } from "lucide-vue-next";
import { Button } from "@/shared/components/ui/button";

const emit = defineEmits<{
  add: [url: string, title: string];
}>();

const url = ref("");
const title = ref("");
const isSubmitting = ref(false);

function handleSubmit() {
  const trimmedUrl = url.value.trim();
  if (!trimmedUrl) return;

  isSubmitting.value = true;
  emit("add", trimmedUrl, title.value.trim() || trimmedUrl);

  url.value = "";
  title.value = "";
  isSubmitting.value = false;
}
</script>

<template>
  <form class="space-y-2" @submit.prevent="handleSubmit">
    <div class="flex gap-2">
      <input
        v-model="url"
        type="url"
        placeholder="Paste YouTube URL…"
        required
        class="flex-1 rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
      />
      <Button type="submit" size="sm" :disabled="!url.trim() || isSubmitting">
        <Loader2 v-if="isSubmitting" class="mr-1 h-4 w-4 animate-spin" />
        <Plus v-else class="mr-1 h-4 w-4" />
        Add
      </Button>
    </div>
    <input
      v-model="title"
      type="text"
      placeholder="Title (optional)"
      class="w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
    />
  </form>
</template>

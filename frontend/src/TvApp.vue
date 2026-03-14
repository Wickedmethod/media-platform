<script setup lang="ts">
import { ref } from "vue";

const status = ref("Connecting…");
</script>

<template>
  <div
    class="flex h-screen w-screen items-center justify-center bg-black text-white"
  >
    <div class="text-center">
      <h1 class="text-4xl font-bold">Media Platform — TV</h1>
      <p class="mt-4 text-lg text-white/60">{{ status }}</p>
    </div>
  </div>
</template>

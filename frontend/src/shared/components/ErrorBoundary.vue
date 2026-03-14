<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue'
import FallbackError from './FallbackError.vue'

const error = ref<Error | null>(null)

onErrorCaptured((err: Error) => {
  error.value = err
  return false
})

function retry() {
  error.value = null
}
</script>

<template>
  <FallbackError v-if="error" :error="error" @retry="retry" />
  <slot v-else />
</template>

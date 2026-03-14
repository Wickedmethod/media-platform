<script setup lang="ts">
import { RouterView } from 'vue-router'
import ErrorBoundary from '@/shared/components/ErrorBoundary.vue'
import ToastContainer from '@/shared/components/ToastContainer.vue'
</script>

<template>
  <ErrorBoundary>
    <RouterView />
  </ErrorBoundary>
  <ToastContainer />
</template>

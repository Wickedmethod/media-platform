<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <div class="flex min-h-screen items-center justify-center">
    <div class="space-y-4 text-center">
      <h1 class="text-4xl font-bold">403</h1>
      <p class="text-muted-foreground">You don't have permission to access this page.</p>
      <div class="flex justify-center gap-3">
        <RouterLink to="/" class="text-primary hover:underline">Go Home</RouterLink>
        <button
          v-if="!auth.isAuthenticated"
          class="text-primary hover:underline"
          @click="auth.logout()"
        >
          Sign In
        </button>
      </div>
    </div>
  </div>
</template>

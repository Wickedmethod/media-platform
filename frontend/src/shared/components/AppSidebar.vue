<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import { ListMusic, Search, Settings } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const auth = useAuthStore()

const navItems: readonly { path: string; label: string; icon: typeof ListMusic; adminOnly?: boolean }[] = [
  { path: '/queue', label: 'Queue', icon: ListMusic },
  { path: '/search', label: 'Search', icon: Search },
  { path: '/admin', label: 'Admin', icon: Settings, adminOnly: true },
]

function isActive(path: string) {
  return route.path.startsWith(path)
}
</script>

<template>
  <aside class="fixed inset-y-0 left-0 z-40 hidden w-56 border-r border-border bg-card md:flex md:flex-col">
    <div class="flex h-14 items-center gap-2 border-b border-border px-4">
      <span class="text-lg font-semibold text-primary">media</span>
      <span class="text-lg font-light text-muted-foreground">::platform</span>
    </div>

    <nav class="flex flex-1 flex-col gap-1 p-3">
      <template v-for="item in navItems" :key="item.path">
        <RouterLink
          v-if="!item.adminOnly || auth.isAdmin"
          :to="item.path"
          class="flex items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          :class="{ 'bg-accent text-accent-foreground': isActive(item.path) }"
        >
          <component :is="item.icon" class="h-4 w-4" />
          {{ item.label }}
        </RouterLink>
      </template>
    </nav>

    <div class="border-t border-border p-3">
      <div class="flex items-center gap-2 text-sm">
        <div class="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
          {{ auth.user?.name?.charAt(0)?.toUpperCase() ?? '?' }}
        </div>
        <span class="truncate text-muted-foreground">{{ auth.user?.name ?? 'Unknown' }}</span>
      </div>
    </div>
  </aside>
</template>

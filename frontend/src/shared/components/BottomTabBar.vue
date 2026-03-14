<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ListMusic, Search, Settings } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const auth = useAuthStore()

const tabs: readonly { path: string; label: string; icon: typeof ListMusic; adminOnly?: boolean }[] = [
  { path: '/queue', label: 'Queue', icon: ListMusic },
  { path: '/search', label: 'Search', icon: Search },
  { path: '/admin', label: 'Admin', icon: Settings, adminOnly: true },
]

const visibleTabs = computed(() =>
  tabs.filter((tab) => !tab.adminOnly || auth.isAdmin),
)

function isActive(path: string) {
  return route.path.startsWith(path)
}
</script>

<template>
  <nav class="fixed inset-x-0 bottom-0 z-50 border-t border-border bg-card md:hidden">
    <div class="flex justify-around safe-area-bottom">
      <RouterLink
        v-for="tab in visibleTabs"
        :key="tab.path"
        :to="tab.path"
        class="flex flex-1 flex-col items-center gap-0.5 py-2 text-muted-foreground transition-colors"
        :class="{ 'text-primary': isActive(tab.path) }"
      >
        <component :is="tab.icon" class="h-5 w-5" />
        <span class="text-[10px] font-medium">{{ tab.label }}</span>
      </RouterLink>
    </div>
  </nav>
</template>

<style scoped>
.safe-area-bottom {
  padding-bottom: env(safe-area-inset-bottom, 0px);
}
</style>

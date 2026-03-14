import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    requiresAdmin?: boolean
    public?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@features/queue/QueueView.vue'),
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@features/admin/AdminView.vue'),
      meta: { requiresAdmin: true },
    },
    {
      path: '/admin/flags',
      name: 'admin-flags',
      component: () => import('@features/admin/FlagPanel.vue'),
      meta: { requiresAdmin: true },
    },
    {
      path: '/unauthorized',
      name: 'unauthorized',
      component: () => import('@shared/views/UnauthorizedView.vue'),
      meta: { public: true },
    },
  ],
})

router.beforeEach((to) => {
  if (to.meta.public) return true

  const auth = useAuthStore()

  if (!auth.isAuthenticated) {
    return { name: 'unauthorized' }
  }

  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'unauthorized' }
  }

  return true
})

export default router

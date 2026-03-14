import { createRouter, createWebHistory } from 'vue-router'

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
    },
  ],
})

export default router

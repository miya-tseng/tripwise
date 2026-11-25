import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/trips/:id',
      name: 'trip-schedule',
      component: () => import('@/views/TripScheduleView.vue'),
      props: true,
    },
  ],
})

export default router

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
      props: (route) => ({ id: route.params.id, day: route.query.day, view: route.query.view }),
    },
  ],
})

export default router

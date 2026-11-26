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
    {
      path: '/trips/:id/overview',
      name: 'trip-overview',
      component: () => import('@/views/TripOverviewView.vue'),
      props: true,
    },
    {
      path: '/trips/:id/checklist',
      name: 'trip-checklist',
      component: () => import('@/views/TripChecklistView.vue'),
      props: true,
    },
  ],
})

export default router

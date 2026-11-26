<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import AppNav from '@/components/AppNav.vue'
import BottomNav from '@/components/BottomNav.vue'
import { sectionOfRoute } from '@/utils/tripNav'

const route = useRoute()

const section = computed(() => sectionOfRoute(route.name))
const tripId = computed(() => (typeof route.params.id === 'string' ? route.params.id : null))
const appSection = computed(() =>
  route.name === 'home' || route.name === 'all-trips' ? route.name : null,
)
</script>

<template>
  <div :class="{ 'has-bottom-nav': (section && tripId) || appSection }">
    <RouterView />
    <BottomNav v-if="section && tripId" :trip-id="tripId" :active="section" />
    <AppNav v-else-if="appSection" :active="appSection" />
  </div>
</template>

<style scoped>
@media (max-width: 720px) {
  .has-bottom-nav {
    padding-bottom: calc(4rem + env(safe-area-inset-bottom));
  }
}

@media (min-width: 721px) {
  .has-bottom-nav {
    padding-left: 5.5rem;
  }
}
</style>

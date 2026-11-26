<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import BottomNav from '@/components/BottomNav.vue'
import { sectionOfRoute } from '@/utils/tripNav'

const route = useRoute()

const section = computed(() => sectionOfRoute(route.name))
const tripId = computed(() => (typeof route.params.id === 'string' ? route.params.id : null))
</script>

<template>
  <div :class="{ 'has-bottom-nav': section && tripId }">
    <RouterView />
    <BottomNav v-if="section && tripId" :trip-id="tripId" :active="section" />
  </div>
</template>

<style scoped>
@media (max-width: 720px) {
  .has-bottom-nav {
    padding-bottom: calc(4rem + env(safe-area-inset-bottom));
  }
}
</style>

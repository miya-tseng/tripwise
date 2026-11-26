<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { TRIP_NAV, tripRoute, type TripSection } from '@/utils/tripNav'

defineProps<{
  tripId: string
  active: TripSection
}>()
</script>

<template>
  <nav class="bottom-nav" aria-label="Trip navigation" data-testid="bottom-nav">
    <RouterLink :to="{ name: 'home' }" class="nav-item" data-testid="nav-trips">
      <span class="nav-icon" aria-hidden="true">🏠</span>
      <span class="nav-label">Trips</span>
    </RouterLink>
    <RouterLink
      v-for="item in TRIP_NAV"
      :key="item.section"
      :to="tripRoute(item.section, tripId)"
      class="nav-item"
      :class="{ active: item.section === active }"
      :aria-current="item.section === active ? 'page' : undefined"
      :data-testid="`nav-${item.section}`"
    >
      <span class="nav-icon" aria-hidden="true">{{ item.icon }}</span>
      <span class="nav-label">{{ item.label }}</span>
    </RouterLink>
  </nav>
</template>

<style scoped>
.bottom-nav {
  display: none;
}

@media (max-width: 720px) {
  .bottom-nav {
    position: fixed;
    inset: auto 0 0;
    z-index: 60;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    padding: 0.3rem 0.4rem calc(0.3rem + env(safe-area-inset-bottom));
    background: rgb(255 255 255 / 0.94);
    border-top: 1px solid var(--color-border);
    box-shadow: 0 -6px 20px -12px rgb(27 36 49 / 0.25);
    backdrop-filter: saturate(1.4) blur(12px);
  }

  .nav-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.1rem;
    min-height: 3rem;
    padding: 0.3rem 0;
    font-size: 0.7rem;
    font-weight: 600;
    color: var(--color-muted);
    text-decoration: none;
    border-radius: 12px;
  }

  .nav-icon {
    font-size: 1.2rem;
    line-height: 1.3;
    filter: grayscale(1);
    opacity: 0.7;
  }

  .nav-item.active {
    color: var(--color-primary);
    background: var(--color-primary-soft);
  }

  .nav-item.active .nav-icon {
    filter: none;
    opacity: 1;
  }
}
</style>

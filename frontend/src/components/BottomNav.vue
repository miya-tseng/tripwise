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
@media (min-width: 721px) {
  .bottom-nav {
    position: fixed;
    inset: 0 auto 0 0;
    z-index: 60;
    display: flex;
    flex-direction: column;
    align-items: stretch;
    gap: 0.35rem;
    width: 5.5rem;
    padding: 1rem 0.5rem;
    background: rgb(255 255 255 / 0.7);
    border-right: 1px solid var(--color-border);
    backdrop-filter: saturate(1.4) blur(12px);
  }

  .nav-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.2rem;
    padding: 0.55rem 0.25rem;
    font-size: 0.72rem;
    font-weight: 600;
    color: var(--color-muted);
    text-align: center;
    text-decoration: none;
    border-radius: 14px;
    transition:
      background 0.15s,
      color 0.15s;
  }

  .nav-item:hover {
    color: var(--color-text);
    background: rgb(27 36 49 / 0.05);
  }

  .nav-icon {
    font-size: 1.35rem;
    line-height: 1.2;
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

  .nav-item:first-child {
    margin-bottom: 0.6rem;
    padding-bottom: 0.9rem;
    border-bottom: 1px solid var(--color-border);
    border-radius: 14px 14px 0 0;
  }
}
</style>

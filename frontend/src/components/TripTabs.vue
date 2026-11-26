<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { TRIP_NAV, tripRoute, type TripSection } from '@/utils/tripNav'

defineProps<{
  tripId: string
  active: TripSection
}>()
</script>

<template>
  <nav class="trip-tabs" aria-label="Trip sections" data-testid="trip-tabs">
    <RouterLink
      v-for="item in TRIP_NAV"
      :key="item.section"
      :to="tripRoute(item.section, tripId)"
      class="tab"
      :class="{ active: item.section === active }"
      :aria-current="item.section === active ? 'page' : undefined"
      :data-testid="`tab-${item.section}`"
    >
      <span aria-hidden="true">{{ item.icon }}</span>
      {{ item.label }}
    </RouterLink>
  </nav>
</template>

<style scoped>
.trip-tabs {
  display: inline-flex;
  gap: 0.25rem;
}

.tab {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.8rem;
  font-size: 0.86rem;
  font-weight: 600;
  color: var(--color-muted);
  text-decoration: none;
  border-radius: 999px;
  transition:
    background 0.15s,
    color 0.15s;
}

.tab:hover {
  color: var(--color-text);
  background: rgb(27 36 49 / 0.05);
}

.tab.active {
  color: var(--color-primary);
  background: var(--color-primary-soft);
}

@media (max-width: 720px) {
  .trip-tabs {
    display: none;
  }
}
</style>

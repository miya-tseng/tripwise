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
  align-self: stretch;
  gap: 1.4rem;
}

.tab {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: -1px;
  padding: 0.55rem 0.1rem 0.6rem;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-muted);
  text-decoration: none;
  border-bottom: 2px solid transparent;
  transition:
    color 0.15s,
    border-color 0.15s;
}

.tab:hover {
  color: var(--color-text);
}

.tab.active {
  color: var(--color-primary);
  border-bottom-color: var(--color-primary);
}

@media (max-width: 720px) {
  .trip-tabs {
    display: none;
  }
}
</style>

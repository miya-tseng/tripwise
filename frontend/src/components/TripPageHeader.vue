<script setup lang="ts">
import { RouterLink } from 'vue-router'
import TripTabs from '@/components/TripTabs.vue'
import type { TripSection } from '@/utils/tripNav'

defineProps<{
  tripId: string
  name: string
  meta?: string
  active: TripSection
}>()
</script>

<template>
  <header class="page-header" data-testid="trip-page-header">
    <div class="header-inner">
      <RouterLink to="/" class="back-link" aria-label="All trips" data-testid="back-link">
        <svg viewBox="0 0 20 20" aria-hidden="true">
          <path
            d="M16 10H5m4-4-4 4 4 4"
            fill="none"
            stroke="currentColor"
            stroke-width="1.7"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
      </RouterLink>
      <div class="title">
        <p class="trip-name" data-testid="header-trip-name">{{ name }}</p>
        <p v-if="meta" class="trip-meta">{{ meta }}</p>
      </div>
    </div>
    <div class="tabs-row">
      <TripTabs :trip-id="tripId" :active="active" />
    </div>
  </header>
</template>

<style scoped>
.page-header {
  position: sticky;
  top: 0;
  z-index: 30;
  background: rgb(246 243 237 / 0.88);
  border-bottom: 1px solid var(--color-border);
  backdrop-filter: saturate(1.4) blur(12px);
}

.header-inner {
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 0.7rem clamp(1rem, 3vw, 2rem) 0.2rem;
}

.tabs-row {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0 clamp(1rem, 3vw, 2rem);
  display: flex;
}

.back-link {
  flex-shrink: 0;
  display: inline-grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  color: var(--color-muted);
  border-radius: 50%;
}

.back-link svg {
  width: 1.15rem;
  height: 1.15rem;
}

.back-link:hover {
  color: var(--color-primary);
  background: rgb(31 92 86 / 0.08);
}

.title {
  flex: 1;
  min-width: 0;
}

.trip-name {
  overflow: hidden;
  font-family: var(--font-display);
  font-size: 1.15rem;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trip-meta {
  font-size: 0.8rem;
  color: var(--color-muted);
}
@media (max-width: 720px) {
  .header-inner {
    padding-bottom: 0.7rem;
  }

  .tabs-row {
    display: none;
  }
}
</style>

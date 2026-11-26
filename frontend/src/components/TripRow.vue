<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import TripActionsMenu from '@/components/TripActionsMenu.vue'
import type { Trip } from '@/types/trip'
import { formatDateRange } from '@/utils/date'
import { coverFor, coverStyle, flagFor } from '@/utils/tripTheme'
import { countdownLabel, daysLabel, progressLabel, tripPhase } from '@/utils/trips'

const props = withDefaults(
  defineProps<{
    trip: Trip
    today: string
    deleting?: boolean
  }>(),
  {
    deleting: false,
  },
)

const emit = defineEmits<{
  delete: [trip: Trip]
}>()

const meta = computed(() => {
  const dates = formatDateRange(props.trip.startDate, props.trip.endDate)
  return props.trip.destination ? `${dates} · ${props.trip.destination.name}` : dates
})
const phase = computed(() => tripPhase(props.trip, props.today))
const status = computed(() => {
  if (phase.value === 'ongoing') {
    return progressLabel(props.trip, props.today)
  }

  return phase.value === 'upcoming'
    ? countdownLabel(props.trip, props.today)
    : daysLabel(props.trip.days)
})
</script>

<template>
  <li class="trip-row" :class="phase" :style="coverStyle(coverFor(trip))" data-testid="trip-row">
    <span class="flag" aria-hidden="true">{{ flagFor(trip) ?? '✈️' }}</span>
    <span class="body">
      <RouterLink
        :to="{ name: 'trip-schedule', params: { id: trip.id } }"
        class="name"
        data-testid="trip-row-link"
      >
        {{ trip.name }}
      </RouterLink>
      <span class="meta" data-testid="trip-row-meta">{{ meta }}</span>
    </span>
    <span class="status" data-testid="trip-row-status">{{ status }}</span>
    <TripActionsMenu
      :trip-name="trip.name"
      :disabled="deleting"
      placement="side"
      @delete="emit('delete', trip)"
    />
  </li>
</template>

<style scoped>
.trip-row {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.8rem;
  min-height: 3.6rem;
  padding: 0.45rem 0.4rem 0.45rem 0.6rem;
  border-radius: 12px;
  transition: background 0.15s;
}

.trip-row:hover {
  background: var(--color-surface-muted);
}

.flag {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 2.5rem;
  height: 2.5rem;
  font-size: 1.15rem;
  background: linear-gradient(150deg, var(--cover-from), var(--cover-to));
  border-radius: 10px;
}

.past .flag {
  filter: grayscale(0.8);
  opacity: 0.75;
}

.body {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.name {
  overflow: hidden;
  font-weight: 600;
  color: var(--color-text);
  text-decoration: none;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.name::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
}

.past .name {
  color: var(--color-muted);
}

.meta {
  overflow: hidden;
  font-size: 0.8rem;
  color: var(--color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status {
  flex-shrink: 0;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-accent);
}

.ongoing .status {
  color: var(--color-primary);
}

.past .status {
  font-weight: 500;
  color: var(--color-subtle);
}
</style>

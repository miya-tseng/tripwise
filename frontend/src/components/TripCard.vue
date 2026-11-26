<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import TripActionsMenu from '@/components/TripActionsMenu.vue'
import type { Trip } from '@/types/trip'
import { dateParts, formatDateRange } from '@/utils/date'
import { coverFor, coverStyle, flagFor } from '@/utils/tripTheme'
import {
  countdownLabel,
  currentDayNumber,
  daysLabel,
  progressLabel,
  type TripPhase,
} from '@/utils/trips'

const props = withDefaults(
  defineProps<{
    trip: Trip
    phase: TripPhase
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

const start = computed(() => dateParts(props.trip.startDate))
const cover = computed(() => coverFor(props.trip))
const flag = computed(() => flagFor(props.trip))
const dates = computed(() => formatDateRange(props.trip.startDate, props.trip.endDate))
const destinationName = computed(() => {
  const destination = props.trip.destination

  if (!destination) {
    return ''
  }

  return destination.address && destination.address !== destination.name
    ? `${destination.name}, ${destination.address}`
    : destination.name
})
const dayNumber = computed(() => currentDayNumber(props.trip, props.today))
const progress = computed(() => Math.round((dayNumber.value / props.trip.days) * 100))
const tripLink = computed(() => ({ name: 'trip-schedule', params: { id: props.trip.id } }))
const todayLink = computed(() => ({ ...tripLink.value, query: { day: String(dayNumber.value) } }))
</script>

<template>
  <li
    class="card trip"
    :class="phase"
    :style="coverStyle(cover)"
    data-testid="trip-item"
    :data-phase="phase"
  >
    <div class="cover" aria-hidden="true" data-testid="trip-cover">
      <span class="cover-flag" data-testid="trip-flag">{{ flag ?? '✈️' }}</span>
      <span class="cover-date">
        <span class="tile-month">{{ start.month }}</span>
        <span class="tile-day">{{ start.day }}</span>
      </span>
    </div>

    <div class="trip-info">
      <span v-if="phase === 'ongoing'" class="now-badge">
        <span class="pulse" aria-hidden="true" />
        Happening now
      </span>
      <span v-else-if="phase === 'upcoming'" class="countdown" data-testid="trip-countdown">
        {{ countdownLabel(trip, today) }}
      </span>
      <RouterLink :to="tripLink" class="trip-name" data-testid="trip-link">
        {{ trip.name }}
      </RouterLink>
      <span class="trip-meta">
        <span v-if="destinationName" class="destination" data-testid="trip-destination">
          <svg viewBox="0 0 20 20" aria-hidden="true">
            <path
              d="M10 18s-5.5-5.2-5.5-9.3a5.5 5.5 0 1 1 11 0C15.5 12.8 10 18 10 18Z"
              fill="currentColor"
            />
            <circle cx="10" cy="8.6" r="2" fill="#fff" />
          </svg>
          <span class="destination-text">{{ destinationName }}</span>
        </span>
        <span class="trip-dates">{{ dates }}</span>
        <span v-if="phase === 'past'" class="trip-length">{{ daysLabel(trip.days) }}</span>
      </span>

      <div v-if="phase === 'ongoing'" class="progress-row">
        <div
          class="progress"
          role="progressbar"
          :aria-valuenow="dayNumber"
          aria-valuemin="1"
          :aria-valuemax="trip.days"
          :aria-label="progressLabel(trip, today)"
        >
          <span class="progress-fill" :style="{ width: `${progress}%` }" />
        </div>
        <span class="progress-label" data-testid="trip-progress">
          {{ progressLabel(trip, today) }}
        </span>
      </div>
    </div>

    <RouterLink
      v-if="phase === 'ongoing'"
      :to="todayLink"
      class="btn btn-primary open-today"
      data-testid="open-today"
    >
      Open today
    </RouterLink>

    <TripActionsMenu
      :trip-name="trip.name"
      :disabled="deleting"
      :placement="phase === 'ongoing' ? 'below' : 'side'"
      @delete="emit('delete', trip)"
    />
  </li>
</template>

<style scoped>
.trip {
  position: relative;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 0.75rem 1rem 1.1rem;
  transition:
    transform 0.18s ease,
    box-shadow 0.18s ease,
    border-color 0.18s ease;
}

.trip:hover {
  transform: translateY(-2px);
  border-color: rgb(31 92 86 / 0.3);
  box-shadow: var(--shadow-lg);
}

.trip:has(.trip-name:focus-visible) {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 4px var(--color-primary-ring);
}

.trip.ongoing {
  padding: 1.4rem 1rem 1.4rem 1.5rem;
  background:
    linear-gradient(120deg, color-mix(in srgb, var(--cover-to) 20%, transparent), transparent 62%),
    var(--color-surface);
  border-color: rgb(31 92 86 / 0.28);
}

.trip.past {
  padding-block: 0.7rem;
  background: rgb(255 255 255 / 0.55);
  box-shadow: none;
}

.trip.past:hover {
  transform: none;
}

.cover {
  position: relative;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  width: 4rem;
  height: 4.4rem;
  padding: 0.4rem 0.45rem;
  overflow: hidden;
  color: #fff;
  background:
    radial-gradient(circle at 85% 15%, rgb(255 255 255 / 0.35), transparent 45%),
    linear-gradient(150deg, var(--cover-from), var(--cover-to));
  border-radius: 14px;
  box-shadow: inset 0 0 0 1px rgb(255 255 255 / 0.18);
}

.cover-flag {
  font-size: 1.15rem;
  line-height: 1;
}

.cover-date {
  display: flex;
  align-items: baseline;
  gap: 0.15rem;
  white-space: nowrap;
  line-height: 1;
  text-shadow: 0 1px 2px rgb(0 0 0 / 0.2);
}

.ongoing .cover {
  width: 5.5rem;
  height: 6rem;
  padding: 0.55rem 0.6rem;
}

.ongoing .cover-flag {
  font-size: 1.6rem;
}

.past .cover {
  width: 3.2rem;
  height: 3.4rem;
  filter: grayscale(0.85);
  opacity: 0.75;
}

.past .cover-flag {
  font-size: 0.9rem;
}

.tile-month {
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.tile-day {
  font-family: var(--font-display);
  font-size: 1.3rem;
  font-weight: 700;
}

.tile-month {
  font-size: 0.56rem;
}

.ongoing .tile-day {
  font-size: 1.7rem;
}

.ongoing .tile-month {
  font-size: 0.66rem;
}

.past .tile-day {
  font-size: 0.95rem;
}

.past .tile-month {
  font-size: 0.5rem;
}

.trip-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.now-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: 0.15rem;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-primary);
}

.pulse {
  width: 0.5rem;
  height: 0.5rem;
  background: var(--color-primary);
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgb(31 92 86 / 0.5);
  animation: pulse 1.8s ease-out infinite;
}

@keyframes pulse {
  to {
    box-shadow: 0 0 0 8px rgb(31 92 86 / 0);
  }
}

.trip-name {
  overflow: hidden;
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: -0.01em;
  color: var(--color-text);
  text-decoration: none;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: color 0.15s;
}

.ongoing .trip-name {
  font-size: 1.6rem;
}

.past .trip-name {
  font-size: 1.02rem;
  color: var(--color-muted);
}

.trip-name::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
}

.trip-name:focus-visible {
  outline: none;
}

.trip:hover .trip-name {
  color: var(--color-primary);
}

.trip-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem 0.9rem;
  font-size: 0.9rem;
  color: var(--color-muted);
}

.destination {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  min-width: 0;
  max-width: 100%;
}

.destination-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trip-dates,
.trip-length {
  white-space: nowrap;
}

.destination svg {
  flex-shrink: 0;
  width: 0.95rem;
  height: 0.95rem;
  color: var(--color-accent);
}

.progress-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 0.6rem;
  max-width: 26rem;
}

.progress {
  flex: 1;
  height: 0.45rem;
  overflow: hidden;
  background: rgb(31 92 86 / 0.12);
  border-radius: 999px;
}

.progress-fill {
  display: block;
  height: 100%;
  background: var(--color-primary);
  border-radius: inherit;
}

.progress-label {
  flex-shrink: 0;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--color-primary);
}

.open-today {
  position: relative;
  z-index: 1;
}

.open-today {
  flex-shrink: 0;
  text-decoration: none;
}

.countdown {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-accent);
}

.trip-length {
  color: var(--color-subtle);
}

@media (max-width: 560px) {
  .ongoing .cover {
    display: none;
  }

  .trip.ongoing {
    flex-wrap: wrap;
  }

  .trip.ongoing .trip-info {
    flex-basis: calc(100% - 3.5rem);
  }

  .trip.ongoing .open-today {
    order: 3;
    flex: 1;
  }

  .ongoing .trip-name {
    font-size: 1.35rem;
  }
}
</style>

<script setup lang="ts">
import { RouterLink } from 'vue-router'
import DayStrip from '@/components/DayStrip.vue'
import FilterMenu from '@/components/FilterMenu.vue'
import type { SelectOption } from '@/components/SelectMenu.vue'
import { VIEW_OPTIONS } from '@/composables/useScheduleView'
import type { Trip } from '@/types/trip'
import type { ScheduleView } from '@/utils/activityTable'
import type { CategoryMeta } from '@/utils/activityDetails'
import { formatDateRange } from '@/utils/date'

defineProps<{
  trip: Trip
  activityCount: number
  budget: string
  ratesNote: string
  scheduleView: ScheduleView
  dates: string[]
  activeDayIndex: number
  today: string
  bookingOptions: SelectOption[]
  booking: string
  categories: CategoryMeta[]
  category: string | null
}>()

const emit = defineEmits<{
  editTrip: []
  addActivity: []
  changeView: [view: ScheduleView]
  jump: [index: number]
  'update:booking': [value: string]
  'update:category': [value: string | null]
}>()
</script>

<template>
  <header class="trip-bar" data-testid="trip-bar">
    <div class="bar-inner">
      <div class="bar-top">
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
        <div class="trip-title">
          <h1 class="display">{{ trip.name }}</h1>
          <p class="trip-meta">
            <span data-testid="trip-dates">{{
              formatDateRange(trip.startDate, trip.endDate)
            }}</span>
            <span aria-hidden="true">·</span>
            <span
              ><span data-testid="trip-days">{{ trip.days }}</span>
              {{ trip.days === 1 ? 'day' : 'days' }}</span
            >
            <span aria-hidden="true">·</span>
            <span
              ><span data-testid="activity-count">{{ activityCount }}</span>
              {{ activityCount === 1 ? 'activity' : 'activities' }}</span
            >
          </p>
        </div>
        <div class="budget" :title="ratesNote">
          <span class="budget-label">Budget</span>
          <span class="budget-value" data-testid="trip-total">{{ budget }}</span>
        </div>
        <div class="bar-actions">
          <button
            type="button"
            class="btn btn-secondary btn-small edit-trip"
            aria-label="Edit trip"
            title="Edit trip"
            data-testid="edit-trip"
            @click="emit('editTrip')"
          >
            <span class="edit-trip-icon" aria-hidden="true">✎</span>
            <span class="edit-trip-label">Edit trip</span>
          </button>
          <button
            type="button"
            class="btn btn-primary btn-small add-activity"
            data-testid="add-activity"
            @click="emit('addActivity')"
          >
            + Add activity
          </button>
        </div>
      </div>

      <div class="bar-bottom">
        <div class="view-toggle" role="group" aria-label="Schedule view">
          <button
            v-for="option in VIEW_OPTIONS"
            :key="option.value"
            type="button"
            :aria-pressed="scheduleView === option.value"
            :data-testid="`view-${option.value}`"
            @click="emit('changeView', option.value)"
          >
            {{ option.label }}
          </button>
        </div>
        <DayStrip
          v-if="scheduleView === 'timeline' && dates.length > 1"
          class="day-strip-row"
          :dates="dates"
          :active-index="activeDayIndex"
          :today="today"
          @jump="emit('jump', $event)"
        />
        <FilterMenu
          :booking="booking"
          @update:booking="emit('update:booking', $event)"
          :category="category"
          @update:category="emit('update:category', $event)"
          class="filters"
          :booking-options="bookingOptions"
          :categories="categories"
        />
      </div>
    </div>
  </header>
</template>

<style scoped>
.trip-bar {
  position: sticky;
  top: 0;
  z-index: 30;
  background: rgb(245 242 236 / 0.86);
  border-bottom: 1px solid var(--color-border);
  backdrop-filter: saturate(1.4) blur(12px);
}

.bar-inner {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0.85rem clamp(1rem, 3vw, 2rem) 0.7rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.bar-top {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.back-link {
  flex-shrink: 0;
  display: inline-grid;
  place-items: center;
  width: 2.2rem;
  height: 2.2rem;
  color: var(--color-muted);
  border-radius: 999px;
  text-decoration: none;
  transition:
    color 0.15s,
    background-color 0.15s;
}

.back-link svg {
  width: 1.15rem;
  height: 1.15rem;
}

.back-link:hover {
  color: var(--color-primary);
  background: rgb(31 92 86 / 0.08);
}

.trip-title {
  flex: 1;
  min-width: 0;
}

h1 {
  overflow: hidden;
  font-size: 1.5rem;
  line-height: 1.2;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trip-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: var(--color-muted);
}

.budget {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  line-height: 1.15;
}

.budget-label {
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--color-subtle);
}

.budget-value {
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.bar-actions {
  display: flex;
  gap: 0.5rem;
}

.bar-bottom {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
}

.bar-bottom .filters {
  margin-left: auto;
}

.bar-bottom .day-strip-row {
  display: none;
}

.view-toggle {
  display: inline-flex;
  padding: 0.2rem;
  background: rgb(27 36 49 / 0.06);
  border-radius: 10px;
}

.view-toggle button {
  padding: 0.3rem 0.8rem;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--color-muted);
  background: none;
  border: none;
  border-radius: 8px;
  cursor: pointer;
}

.view-toggle button[aria-pressed='true'] {
  color: var(--color-text);
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
}

.edit-trip {
  width: 2.25rem;
  padding: 0;
  font-size: 1rem;
}

.edit-trip-label {
  display: none;
}

@media (max-width: 720px) {
  .bar-inner {
    padding: 0.5rem 0.75rem 0.5rem;
    gap: 0.45rem;
  }

  .bar-top {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto auto;
    grid-template-areas: 'back title budget actions';
    gap: 0.5rem;
  }

  .back-link {
    grid-area: back;
  }

  .trip-title {
    grid-area: title;
    min-width: 0;
  }

  h1 {
    overflow: hidden;
    font-size: 1.1rem;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .trip-meta {
    font-size: 0.72rem;
  }

  .trip-meta > :nth-child(n + 2) {
    display: none;
  }

  .budget {
    grid-area: budget;
  }

  .budget-label {
    font-size: 0.6rem;
  }

  .budget-value {
    font-size: 0.95rem;
  }

  .bar-actions {
    grid-area: actions;
  }

  .bar-actions .add-activity {
    display: none;
  }

  .bar-bottom {
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .bar-inner {
    padding-bottom: 0.5rem;
  }

  .bar-bottom .day-strip-row {
    display: flex;
    order: 3;
    flex-basis: 100%;
  }
}
</style>

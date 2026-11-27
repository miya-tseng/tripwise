<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import ActivityItem from '@/components/ActivityItem.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import DayStayRow from '@/components/DayStayRow.vue'
import DayMap, { type DayMapStop } from '@/components/DayMap.vue'
import type { Activity, ScheduleDay } from '@/types/activity'
import type { Stay } from '@/types/stay'
import { dateParts, formatDayLabel } from '@/utils/date'
import { dayCosts, dayTotalInTwd, formatTotals, formatTwd, totalsByCurrency } from '@/utils/money'
import { dayRouteLink, directionsLink, placesOf, previousPlaceOf } from '@/utils/mapsLinks'
import { hasMapsKey } from '@/utils/googleMaps'

withDefaults(
  defineProps<{
    days: ScheduleDay[]
    editingId?: string | null
    deletingId?: string | null
    today?: string | null
    activeDate?: string | null
  }>(),
  {
    editingId: null,
    deletingId: null,
    today: null,
    activeDate: null,
  },
)

const emit = defineEmits<{
  edit: [activity: Activity]
  delete: [id: string]
  suggest: [activity: Activity]
  add: [date: string]
  addStay: [date: string]
  editStay: [stay: Stay]
  extendStay: [stay: Stay]
}>()

const activityToDelete = ref<Activity | null>(null)
const mapsAvailable = hasMapsKey()
const openMaps = ref(new Set<string>())
const highlightedId = ref<string | null>(null)

function stopsOf(day: ScheduleDay): DayMapStop[] {
  return day.activities.flatMap((activity) =>
    activity.place ? [{ activityId: activity.id, name: activity.name, place: activity.place }] : [],
  )
}

function canShowMap(day: ScheduleDay): boolean {
  return mapsAvailable && stopsOf(day).length > 0
}

function toggleMap(date: string) {
  const next = new Set(openMaps.value)

  if (next.has(date)) {
    next.delete(date)
  } else {
    next.add(date)
  }

  openMaps.value = next
}

async function selectFromMap(activityId: string) {
  highlightedId.value = activityId
  await nextTick()
  document
    .querySelector(`[data-activity-id="${CSS.escape(activityId)}"]`)
    ?.scrollIntoView?.({ block: 'nearest', behavior: 'smooth' })
}

function directionsFor(activities: Activity[], index: number): string | null {
  const place = activities[index]?.place
  const previous = previousPlaceOf(activities, index)
  return place && previous ? directionsLink(previous, place) : null
}

function previousPlaceName(activities: Activity[], index: number): string {
  return previousPlaceOf(activities, index)?.name ?? ''
}

function dayTotalText(day: ScheduleDay): string {
  const total = dayTotalInTwd(day)
  return total === null ? formatTotals(totalsByCurrency(dayCosts(day))) : formatTwd(total)
}

const confirmMessage = computed(() =>
  activityToDelete.value ? `Delete "${activityToDelete.value.name}" from your schedule?` : '',
)

function confirmDelete() {
  if (activityToDelete.value) {
    emit('delete', activityToDelete.value.id)
  }
  activityToDelete.value = null
}
</script>

<template>
  <ol class="days">
    <li
      v-for="(day, index) in days"
      :key="day.date"
      class="card day"
      :class="{ 'is-today': day.date === today, 'off-page': activeDate && day.date !== activeDate }"
      :aria-current="day.date === today ? 'date' : undefined"
      data-testid="schedule-day"
    >
      <header class="day-header">
        <div class="date-tile" aria-hidden="true">
          <span class="tile-month">{{ dateParts(day.date).month }}</span>
          <span class="tile-day">{{ dateParts(day.date).day }}</span>
        </div>
        <div class="day-heading">
          <span class="eyebrow">
            Day {{ index + 1 }}
            <span v-if="day.date === today" class="today-badge" data-testid="today-badge"
              >Today</span
            >
          </span>
          <h3 class="day-date" data-testid="day-date">{{ formatDayLabel(day.date) }}</h3>
        </div>
        <button
          type="button"
          class="btn btn-secondary btn-small add-to-day"
          :aria-label="`Add activity to Day ${index + 1}`"
          data-testid="add-to-day"
          @click="emit('add', day.date)"
        >
          + Add
        </button>
        <div class="day-summary">
          <span class="day-total" data-testid="day-total">{{ dayTotalText(day) }}</span>
          <span class="day-count">
            {{ day.activities.length }}
            {{ day.activities.length === 1 ? 'activity' : 'activities' }}
          </span>
          <button
            v-if="canShowMap(day)"
            type="button"
            class="map-toggle"
            :aria-expanded="openMaps.has(day.date)"
            data-testid="toggle-day-map"
            @click="toggleMap(day.date)"
          >
            {{ openMaps.has(day.date) ? 'Hide map' : 'Show map' }}
          </button>
          <a
            v-if="dayRouteLink(placesOf(day.activities))"
            class="day-route"
            :href="dayRouteLink(placesOf(day.activities))!"
            target="_blank"
            rel="noopener noreferrer"
            data-testid="day-route-link"
          >
            Day route ↗
          </a>
        </div>
      </header>

      <DayStayRow
        v-if="day.checkingOut"
        :day="day"
        kind="checkout"
        @edit-stay="emit('editStay', $event)"
      />

      <DayMap
        v-if="canShowMap(day) && openMaps.has(day.date)"
        :stops="stopsOf(day)"
        :highlighted-id="highlightedId"
        @select="selectFromMap"
      />

      <button
        v-if="day.activities.length === 0"
        type="button"
        class="day-empty"
        data-testid="day-empty"
        @click="emit('add', day.date)"
      >
        Nothing planned yet.
        <span class="day-empty-action">+ Add an activity</span>
      </button>
      <ul v-else class="timeline">
        <template v-for="(activity, activityIndex) in day.activities" :key="activity.id">
          <li
            v-if="directionsFor(day.activities, activityIndex)"
            class="directions"
            data-testid="directions"
          >
            <a
              :href="directionsFor(day.activities, activityIndex)!"
              target="_blank"
              rel="noopener noreferrer"
              data-testid="directions-link"
            >
              <span class="directions-full"
                >Directions from {{ previousPlaceName(day.activities, activityIndex) }}</span
              >
              <span class="directions-short">🧭 Directions</span> ↗
            </a>
          </li>
          <ActivityItem
            :activity="activity"
            :editing="activity.id === editingId"
            :highlighted="activity.id === highlightedId"
            :deleting="deletingId === activity.id"
            @edit="emit('edit', $event)"
            @delete="activityToDelete = $event"
            @suggest="emit('suggest', $event)"
          />
        </template>
      </ul>

      <DayStayRow
        :day="day"
        kind="tonight"
        @add-stay="emit('addStay', $event)"
        @edit-stay="emit('editStay', $event)"
        @extend-stay="emit('extendStay', $event)"
      />
    </li>
  </ol>

  <ConfirmDialog
    :open="activityToDelete !== null"
    title="Delete activity?"
    :message="confirmMessage"
    confirm-label="Delete"
    @confirm="confirmDelete"
    @cancel="activityToDelete = null"
  />
</template>

<style scoped>
.days {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.day.is-today {
  border-color: var(--color-primary);
  box-shadow:
    0 0 0 3px var(--color-primary-ring),
    var(--shadow-lg);
}

.day.is-today .date-tile {
  color: #fff;
  background: var(--color-primary);
}

.today-badge {
  display: inline-block;
  margin-left: 0.4rem;
  padding: 0.1rem 0.45rem;
  font-size: 0.68rem;
  letter-spacing: 0.06em;
  color: #fff;
  background: var(--color-primary);
  border-radius: 999px;
  vertical-align: 1px;
}

.day {
  scroll-margin-top: calc(var(--header-offset, 7.5rem) + 0.75rem);
  padding: 1.25rem 1.4rem 1.35rem;
}

.day-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.date-tile {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 3.4rem;
  height: 3.6rem;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 12px;
  line-height: 1;
}

.tile-month {
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.tile-day {
  margin-top: 0.2rem;
  font-family: var(--font-display);
  font-size: 1.55rem;
  font-weight: 700;
}

.day-heading {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}

.day-date {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.day-summary {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.05rem;
  text-align: right;
}

.day-total {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.day-count {
  font-size: 0.78rem;
  color: var(--color-subtle);
}

.add-to-day {
  flex-shrink: 0;
}

.day-empty-action {
  margin-left: 0.4rem;
  font-weight: 600;
  color: var(--color-primary);
}

.day-empty {
  display: block;
  width: calc(100% - 4.4rem);
  text-align: left;
  background: none;
  cursor: pointer;
  font: inherit;
  margin: 1rem 0 0 4.4rem;
  padding: 0.85rem 1rem;
  font-size: 0.9rem;
  color: var(--color-subtle);
  border: 1.5px dashed var(--color-border);
  border-radius: var(--radius-sm);
}

.timeline {
  position: relative;
  list-style: none;
  margin: 1rem 0 0;
  padding: 0 0 0 4.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.timeline::before {
  content: '';
  position: absolute;
  top: 1.1rem;
  bottom: 1.1rem;
  left: 1.7rem;
  width: 2px;
  background: linear-gradient(var(--color-primary-soft), var(--color-border));
  border-radius: 2px;
}

.directions {
  position: relative;
  margin: -0.1rem 0;
  padding: 0 0 0 1rem;
  font-size: 0.78rem;
}

.directions a {
  color: var(--color-primary);
  font-weight: 600;
  text-decoration: none;
}

.directions a:hover {
  text-decoration: underline;
}

.map-toggle {
  margin-top: 0.2rem;
  padding: 0;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--color-accent);
  background: none;
  border: none;
  cursor: pointer;
}

.map-toggle:hover {
  text-decoration: underline;
}

.day-route {
  margin-top: 0.2rem;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
}

.day-route:hover {
  text-decoration: underline;
}

.day.off-page {
  display: none;
}

.directions-short {
  display: none;
}

@media (max-width: 560px) {
  .directions-full {
    display: none;
  }

  .directions-short {
    display: inline;
  }

  .day {
    padding: 1rem 0.9rem 1.1rem;
  }

  .day-header {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    grid-template-areas: 'tile heading summary';
    gap: 0.6rem 0.75rem;
  }

  .date-tile {
    grid-area: tile;
    width: 3rem;
    height: 3.2rem;
  }

  .day-heading {
    grid-area: heading;
  }

  .day-date {
    font-size: 1.05rem;
  }

  .day-summary {
    grid-area: summary;
  }

  .day-total {
    font-size: 1.05rem;
  }

  .add-to-day {
    display: none;
  }

  .timeline {
    padding-left: 1.4rem;
  }

  .timeline::before {
    left: 0.4rem;
  }

  .day-empty {
    width: 100%;
    margin-left: 0;
  }
}
</style>

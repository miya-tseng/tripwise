<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import DayMap, { type DayMapStop } from '@/components/DayMap.vue'
import type { Activity, ScheduleDay } from '@/types/activity'
import type { Stay } from '@/types/stay'
import { dateParts, formatDayLabel } from '@/utils/date'
import { HOME_CURRENCY } from '@/types/currency'
import {
  dayCosts,
  dayTotalInTwd,
  formatCurrency,
  formatTotals,
  formatTwd,
  totalsByCurrency,
} from '@/utils/money'
import {
  dayRouteLink,
  directionsLink,
  placeLink,
  placesOf,
  previousPlaceOf,
} from '@/utils/mapsLinks'
import { hasMapsKey } from '@/utils/googleMaps'
import { bookingStatusMeta, categoryMeta, notesPreview } from '@/utils/activityDetails'
import { formatTime } from '@/utils/schedule'
import { nightNumber } from '@/utils/stays'

withDefaults(
  defineProps<{
    days: ScheduleDay[]
    editingId?: string | null
    deletingId?: string | null
    today?: string | null
  }>(),
  {
    editingId: null,
    deletingId: null,
    today: null,
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

function isForeign(activity: Activity): boolean {
  return activity.cost > 0 && activity.currency !== HOME_CURRENCY
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
      :class="{ 'is-today': day.date === today }"
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

      <button
        v-if="day.checkingOut"
        type="button"
        class="stay-row checkout"
        data-testid="day-checkout"
        @click="emit('editStay', day.checkingOut)"
      >
        <span class="stay-icon" aria-hidden="true">🧳</span>
        <span class="stay-label">Check out</span>
        <span class="stay-name">{{ day.checkingOut.name }}</span>
      </button>

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
              Directions from {{ previousPlaceName(day.activities, activityIndex) }} ↗
            </a>
          </li>
          <li
            class="activity"
            :class="[
              `category-${activity.category.toLowerCase()}`,
              {
                editing: activity.id === editingId,
                highlighted: activity.id === highlightedId,
              },
            ]"
            :data-activity-id="activity.id"
            data-testid="activity-item"
          >
            <span class="dot" aria-hidden="true" />
            <span class="activity-time" data-testid="activity-time">
              {{ formatTime(activity.startTime) }} – {{ formatTime(activity.endTime) }}
            </span>
            <span class="activity-main">
              <span class="activity-title">
                <span class="activity-heading">
                  <span
                    class="category-icon"
                    :title="categoryMeta(activity.category).label"
                    data-testid="activity-category"
                    >{{ categoryMeta(activity.category).icon }}</span
                  >
                  <span class="activity-name" data-testid="activity-name">{{ activity.name }}</span>
                </span>
                <span
                  v-if="activity.bookingStatus !== 'NotNeeded'"
                  class="booking-badge"
                  :class="`tone-${bookingStatusMeta(activity.bookingStatus).tone}`"
                  data-testid="booking-badge"
                >
                  {{ bookingStatusMeta(activity.bookingStatus).label }}
                </span>
              </span>
              <span v-if="activity.place" class="activity-place">
                <a
                  v-if="activity.place"
                  class="map-link"
                  :href="placeLink(activity.place)"
                  target="_blank"
                  rel="noopener noreferrer"
                  :aria-label="`Open ${activity.name} in Google Maps`"
                  data-testid="activity-map-link"
                >
                  <svg viewBox="0 0 20 20" aria-hidden="true">
                    <path
                      d="M10 18s-5.5-5.2-5.5-9.3a5.5 5.5 0 1 1 11 0C15.5 12.8 10 18 10 18Z"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.6"
                    />
                    <circle cx="10" cy="8.6" r="1.9" fill="currentColor" />
                  </svg>
                </a>
                <span class="activity-address" data-testid="activity-address">
                  {{ activity.place.address }}
                </span>
              </span>
              <span v-if="activity.notes" class="activity-notes" data-testid="activity-notes">
                {{ notesPreview(activity.notes) }}
              </span>
              <a
                v-if="activity.link"
                class="activity-link"
                :href="activity.link"
                target="_blank"
                rel="noopener noreferrer"
                data-testid="activity-link"
              >
                {{
                  activity.confirmationCode ? `Booking ${activity.confirmationCode}` : 'Open link'
                }}
                ↗
              </a>
              <span
                v-else-if="activity.confirmationCode"
                class="activity-confirmation"
                data-testid="activity-confirmation"
              >
                Booking {{ activity.confirmationCode }}
              </span>
            </span>
            <span class="activity-cost" :class="{ free: activity.cost === 0 }">
              <span data-testid="activity-cost">
                {{
                  activity.cost === 0 ? 'Free' : formatCurrency(activity.cost, activity.currency)
                }}
              </span>
              <span
                v-if="isForeign(activity) && activity.costInTwd !== null"
                class="activity-cost-twd"
                data-testid="activity-cost-twd"
              >
                ≈ {{ formatTwd(activity.costInTwd) }}
              </span>
            </span>
            <div class="activity-actions">
              <button
                v-if="activity.place"
                type="button"
                class="btn btn-ghost btn-small ideas"
                data-testid="suggest-activity"
                @click="emit('suggest', activity)"
              >
                Ideas
              </button>
              <button
                type="button"
                class="btn btn-ghost btn-small"
                data-testid="edit-activity"
                @click="emit('edit', activity)"
              >
                Edit
              </button>
              <button
                type="button"
                class="btn btn-ghost btn-small delete"
                data-testid="delete-activity"
                :disabled="deletingId === activity.id"
                @click="activityToDelete = activity"
              >
                {{ deletingId === activity.id ? 'Deleting...' : 'Delete' }}
              </button>
            </div>
          </li>
        </template>
      </ul>

      <button
        v-if="day.stay"
        type="button"
        class="stay-row tonight"
        data-testid="day-stay"
        @click="emit('editStay', day.stay)"
      >
        <span class="stay-icon" aria-hidden="true">🌙</span>
        <span class="stay-label">Tonight</span>
        <span class="stay-name" data-testid="day-stay-name">{{ day.stay.name }}</span>
        <span class="stay-night">
          Night {{ nightNumber(day.stay, day.date) }} of {{ day.stay.nights }}
        </span>
        <span
          v-if="day.stay.bookingStatus !== 'NotNeeded'"
          class="booking-badge"
          :class="`tone-${bookingStatusMeta(day.stay.bookingStatus).tone}`"
        >
          {{ bookingStatusMeta(day.stay.bookingStatus).label }}
        </span>
      </button>
      <div v-else-if="day.checkingOut" class="stay-row missing quick" data-testid="day-no-stay">
        <span class="stay-icon" aria-hidden="true">🌙</span>
        <span class="stay-label">Tonight</span>
        <span class="stay-name">No stay yet</span>
        <button
          type="button"
          class="extend-stay"
          data-testid="day-extend-stay"
          @click="emit('extendStay', day.checkingOut)"
        >
          Same as last night · {{ day.checkingOut.name }}
        </button>
        <button
          type="button"
          class="stay-action stay-action-button"
          data-testid="day-add-stay"
          @click="emit('addStay', day.date)"
        >
          + Add stay
        </button>
      </div>
      <button
        v-else
        type="button"
        class="stay-row missing"
        data-testid="day-add-stay"
        @click="emit('addStay', day.date)"
      >
        <span class="stay-icon" aria-hidden="true">🌙</span>
        <span class="stay-label">Tonight</span>
        <span class="stay-name">No stay yet</span>
        <span class="stay-action">+ Add stay</span>
      </button>
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
.stay-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  margin-top: 0.9rem;
  padding: 0.6rem 0.85rem;
  font: inherit;
  font-size: 0.88rem;
  text-align: left;
  color: var(--color-text);
  background: var(--color-surface-muted, rgb(245 241 234));
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md, 10px);
  cursor: pointer;
}

.stay-row:hover {
  border-color: var(--color-primary);
}

.stay-row.tonight {
  background: rgb(46 74 98 / 0.07);
  border-color: rgb(46 74 98 / 0.18);
}

.stay-row.missing {
  color: var(--color-muted);
  background: transparent;
  border-style: dashed;
}

.stay-row.checkout {
  margin-top: 0.6rem;
  margin-bottom: 0.2rem;
  background: transparent;
}

.stay-label {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.stay-name {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.stay-night {
  font-size: 0.8rem;
  color: var(--color-muted);
}

.stay-action {
  margin-left: auto;
  font-weight: 600;
  color: var(--color-primary);
}

.stay-row.quick {
  cursor: default;
}

.stay-row.quick:hover {
  border-color: var(--color-border);
}

.extend-stay {
  max-width: 100%;
  padding: 0.25rem 0.7rem;
  overflow: hidden;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border: 1px solid rgb(31 92 86 / 0.25);
  border-radius: 999px;
  cursor: pointer;
}

.extend-stay:hover {
  color: #fff;
  background: var(--color-primary);
}

.stay-action-button {
  padding: 0.2rem 0.3rem;
  font: inherit;
  font-weight: 600;
  background: none;
  border: 0;
  border-radius: 6px;
  cursor: pointer;
}

.stay-action-button:hover {
  text-decoration: underline;
}

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
  scroll-margin-top: 8.5rem;
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

.activity {
  position: relative;
  display: flex;
  align-items: center;
  gap: 1.1rem;
  min-height: 3rem;
  padding: 0.45rem 0.5rem 0.45rem 1rem;
  border: 1px solid transparent;
  border-radius: 12px;
  transition:
    background-color 0.15s,
    border-color 0.15s;
}

.activity:hover {
  background: var(--color-surface-muted);
  border-color: var(--color-border);
}

.activity.editing {
  background: var(--color-primary-soft);
  border-color: rgb(31 92 86 / 0.25);
}

.dot {
  position: absolute;
  left: -2.95rem;
  top: 50%;
  width: 0.75rem;
  height: 0.75rem;
  margin-top: -0.375rem;
  background: var(--color-surface);
  border: 2.5px solid var(--category-color, var(--color-primary));
  border-radius: 50%;
  box-shadow: 0 0 0 4px var(--color-surface);
}

.activity.editing .dot {
  background: var(--color-primary);
}

.activity-time {
  flex-shrink: 0;
  min-width: 7.2rem;
  font-size: 0.9rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-muted);
}

.activity-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.activity-title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.15rem 0.4rem;
  min-width: 0;
}

.category-icon {
  flex-shrink: 0;
  font-size: 0.95rem;
}

.activity-title .activity-name {
  min-width: 0;
  white-space: normal;
  overflow-wrap: anywhere;
}

.activity-notes {
  overflow: hidden;
  font-size: 0.78rem;
  color: var(--color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-link,
.activity-confirmation {
  align-self: flex-start;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
}

.activity-link:hover {
  text-decoration: underline;
}

.activity-address {
  overflow: hidden;
  font-size: 0.78rem;
  color: var(--color-subtle);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-name {
  min-width: 0;
  overflow: hidden;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-heading {
  min-width: 0;
}

.activity-heading .category-icon {
  margin-right: 0.3rem;
}

.activity-place {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  min-width: 0;
}

.map-link {
  flex-shrink: 0;
  display: grid;
  place-items: center;
  width: 1.5rem;
  height: 1.5rem;
  color: var(--color-accent);
  border-radius: 8px;
  transition: background-color 0.15s;
}

.map-link svg {
  width: 0.95rem;
  height: 0.95rem;
}

.map-link:hover,
.map-link:focus-visible {
  outline: none;
  background: var(--color-accent-soft);
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

.activity.highlighted {
  background: var(--color-accent-soft);
  border-color: rgb(201 113 75 / 0.35);
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

.activity-cost {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  min-width: 5.5rem;
  font-size: 0.92rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  text-align: right;
  color: var(--color-accent);
}

.activity-cost-twd {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-subtle);
}

.activity-cost.free {
  font-weight: 500;
  color: var(--color-subtle);
}

.activity-actions {
  display: flex;
  gap: 0.15rem;
  opacity: 0.55;
  transition: opacity 0.15s;
}

.activity:hover .activity-actions,
.activity:focus-within .activity-actions,
.activity.editing .activity-actions {
  opacity: 1;
}

.ideas {
  color: var(--color-accent);
}

.ideas:hover:not(:disabled) {
  color: var(--color-accent);
  background: var(--color-accent-soft);
}

.delete:hover:not(:disabled) {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}

@media (hover: none) {
  .activity-actions {
    opacity: 1;
  }
}

@media (max-width: 560px) {
  .day {
    padding: 1rem 0.9rem 1.1rem;
  }

  .day-header {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    grid-template-areas:
      'tile heading summary'
      'add add add';
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
    grid-area: add;
    width: 100%;
  }

  .timeline {
    padding-left: 1.4rem;
  }

  .timeline::before {
    left: 0.4rem;
  }

  .dot {
    left: -1.35rem;
  }

  .day-empty {
    width: 100%;
    margin-left: 0;
  }

  .activity {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    grid-template-areas:
      'time cost'
      'name cost'
      'actions actions';
    gap: 0.15rem 0.6rem;
    padding: 0.55rem 0.4rem 0.4rem 0.6rem;
  }

  .activity-time {
    grid-area: time;
    min-width: 0;
    font-size: 0.8rem;
  }

  .activity-main {
    grid-area: name;
  }

  .activity-cost {
    grid-area: cost;
    align-self: start;
    min-width: 0;
  }

  .activity-actions {
    grid-area: actions;
    justify-content: flex-end;
    opacity: 1;
  }
}
</style>

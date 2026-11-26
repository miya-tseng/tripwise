<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { storeToRefs } from 'pinia'
import ActivityForm from '@/components/ActivityForm.vue'
import ScheduleDays from '@/components/ScheduleDays.vue'
import TripEditDialog from '@/components/TripEditDialog.vue'
import { useScheduleStore } from '@/stores/schedule'
import type { Activity, SaveActivityInput } from '@/types/activity'
import type { GeoPoint } from '@/types/place'
import type { UpdateTripInput } from '@/types/trip'
import { formatDateRange } from '@/utils/date'
import { formatTotals, formatTwd, totalsByCurrency, tripTotalInTwd } from '@/utils/money'
import { lastUsedCurrency, rememberCurrency } from '@/utils/preferences'

const props = defineProps<{
  id: string
}>()

const store = useScheduleStore()
const {
  trip,
  days,
  loading,
  loadError,
  error,
  deletingId,
  editTripError,
  ratesAvailable,
  ratesUpdatedAt,
  exchangeRates,
} = storeToRefs(store)

const editing = ref<Activity | null>(null)
const tripEditorOpen = ref(false)

const dates = computed(() => days.value.map((day) => day.date))
const defaultCurrency = ref(lastUsedCurrency(props.id))
const searchNear = computed<GeoPoint | null>(() => {
  const places = days.value.flatMap((day) =>
    day.activities.flatMap((activity) => activity.place ?? []),
  )
  const last = places.at(-1)
  return last ? { latitude: last.latitude, longitude: last.longitude } : null
})
const budgetInTwd = computed(() => tripTotalInTwd(days.value))
const budget = computed(() =>
  budgetInTwd.value === null
    ? formatTotals(totalsByCurrency(days.value.flatMap((day) => day.activities)))
    : formatTwd(budgetInTwd.value),
)
const ratesNote = computed(() => {
  if (!ratesAvailable.value) {
    return 'Exchange rates are unavailable right now, so totals are shown per currency.'
  }

  const updated = ratesUpdatedAt.value
    ? new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(
        new Date(ratesUpdatedAt.value),
      )
    : null
  return updated ? `Totals in TWD · Rates by Exchange Rate API, updated ${updated}` : ''
})
const activityCount = computed(() =>
  days.value.reduce((total, day) => total + day.activities.length, 0),
)

watch(
  () => props.id,
  (id) => {
    editing.value = null
    defaultCurrency.value = lastUsedCurrency(id)
    store.load(id)
  },
  { immediate: true },
)

async function save(input: SaveActivityInput): Promise<boolean> {
  const saved = editing.value ? await store.update(editing.value.id, input) : await store.add(input)

  if (!saved) {
    return false
  }

  editing.value = null

  if (input.cost > 0) {
    rememberCurrency(props.id, input.currency)
    defaultCurrency.value = input.currency
  }

  return true
}

function startEditing(activity: Activity) {
  store.clearError()
  editing.value = activity
}

function cancelEditing() {
  store.clearError()
  editing.value = null
}

function openTripEditor() {
  store.clearEditTripError()
  tripEditorOpen.value = true
}

function closeTripEditor() {
  tripEditorOpen.value = false
}

async function saveTrip(input: UpdateTripInput): Promise<boolean> {
  const saved = await store.editTrip(input)

  if (saved) {
    editing.value = null
  }

  return saved
}

async function removeActivity(id: string) {
  if (editing.value?.id === id) {
    editing.value = null
  }

  await store.remove(id)
}
</script>

<template>
  <main class="page">
    <RouterLink to="/" class="back-link" data-testid="back-link">
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
      All trips
    </RouterLink>

    <div v-if="loading" class="skeleton" aria-busy="true">
      <p class="loading">Loading...</p>
    </div>

    <div v-else-if="loadError" role="alert" class="alert" data-testid="load-error">
      {{ loadError }}
    </div>

    <template v-else-if="trip">
      <header class="hero">
        <div class="hero-text">
          <span class="hero-eyebrow">Trip itinerary</span>
          <h1 class="display">{{ trip.name }}</h1>
          <p class="hero-dates" data-testid="trip-dates">
            {{ formatDateRange(trip.startDate, trip.endDate) }}
          </p>
          <button type="button" class="hero-edit" data-testid="edit-trip" @click="openTripEditor">
            <svg viewBox="0 0 20 20" aria-hidden="true">
              <path
                d="m12.5 4.5 3 3M4 16l.8-3.6 8.5-8.5a1.4 1.4 0 0 1 2 0l.8.8a1.4 1.4 0 0 1 0 2l-8.5 8.5L4 16Z"
                fill="none"
                stroke="currentColor"
                stroke-width="1.6"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
            Edit trip
          </button>
        </div>
        <dl class="hero-stats">
          <div class="stat">
            <dt>Days</dt>
            <dd data-testid="trip-days">{{ trip.days }}</dd>
          </div>
          <div class="stat">
            <dt>{{ activityCount === 1 ? 'Activity' : 'Activities' }}</dt>
            <dd data-testid="activity-count">{{ activityCount }}</dd>
          </div>
          <div class="stat stat-budget">
            <dt>Budget</dt>
            <dd data-testid="trip-total">{{ budget }}</dd>
          </div>
        </dl>
      </header>

      <div class="layout">
        <section class="card form-panel" :class="{ editing }">
          <span class="eyebrow">{{ editing ? 'Editing' : 'Plan' }}</span>
          <h2 class="section-title">
            {{ editing ? `Edit "${editing.name}"` : 'Add activity' }}
          </h2>
          <ActivityForm
            :key="editing?.id ?? 'new'"
            :dates="dates"
            :activity="editing"
            :submit="save"
            :rates="exchangeRates"
            :default-currency="defaultCurrency"
            :near="searchNear"
            @cancel="cancelEditing"
          />
          <div v-if="error" role="alert" class="alert form-alert" data-testid="schedule-error">
            <span>{{ error }}</span>
            <button
              type="button"
              class="alert-close"
              aria-label="Dismiss"
              data-testid="dismiss-error"
              @click="store.clearError"
            >
              ✕
            </button>
          </div>
        </section>

        <section class="schedule-panel">
          <div class="schedule-header">
            <h2 class="section-title schedule-title">Schedule</h2>
            <p
              v-if="ratesNote"
              class="rates-note"
              :class="{ warning: !ratesAvailable }"
              data-testid="rates-note"
            >
              {{ ratesNote }}
            </p>
          </div>
          <ScheduleDays
            :days="days"
            :editing-id="editing?.id ?? null"
            :deleting-id="deletingId"
            @edit="startEditing"
            @delete="removeActivity"
          />
        </section>
      </div>
      <TripEditDialog
        :open="tripEditorOpen"
        :trip="trip"
        :submit="saveTrip"
        :error="editTripError"
        @close="closeTripEditor"
      />
    </template>
  </main>
</template>

<style scoped>
.page {
  max-width: 1240px;
  margin: 0 auto;
  padding: 1.75rem clamp(1.25rem, 3.5vw, 2.75rem) 4.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.back-link {
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.75rem 0.35rem 0.55rem;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-muted);
  text-decoration: none;
  border-radius: 999px;
  transition:
    color 0.15s,
    background-color 0.15s;
}

.back-link svg {
  width: 1.05rem;
  height: 1.05rem;
}

.back-link:hover {
  color: var(--color-primary);
  background: rgb(31 92 86 / 0.08);
}

.hero {
  position: relative;
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 2rem;
  padding: clamp(1.75rem, 4vw, 2.75rem);
  overflow: hidden;
  color: #fff;
  background:
    radial-gradient(520px 260px at 100% 0%, rgb(201 113 75 / 0.45) 0%, transparent 70%),
    radial-gradient(420px 300px at 0% 120%, rgb(126 186 170 / 0.35) 0%, transparent 70%),
    linear-gradient(135deg, #1a4f4a 0%, #1f5c56 45%, #2a6b5f 100%);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

.hero::after {
  content: '';
  position: absolute;
  right: -4rem;
  bottom: -6rem;
  width: 18rem;
  height: 18rem;
  border: 1.5px solid rgb(255 255 255 / 0.12);
  border-radius: 50%;
  box-shadow:
    0 0 0 2.5rem rgb(255 255 255 / 0.035),
    0 0 0 5rem rgb(255 255 255 / 0.025);
  pointer-events: none;
}

.hero-text {
  position: relative;
  z-index: 1;
  min-width: 0;
}

.hero-eyebrow {
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: #f3c9b3;
}

h1 {
  margin-top: 0.35rem;
  font-size: clamp(2rem, 4.5vw, 3rem);
  line-height: 1.1;
  overflow-wrap: anywhere;
}

.hero-dates {
  margin-top: 0.6rem;
  font-size: 1.05rem;
  color: rgb(255 255 255 / 0.78);
}

.hero-edit {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-top: 1rem;
  padding: 0.4rem 0.85rem 0.4rem 0.7rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: #fff;
  background: rgb(255 255 255 / 0.12);
  border: 1px solid rgb(255 255 255 / 0.22);
  border-radius: 999px;
  cursor: pointer;
  transition:
    background-color 0.15s,
    border-color 0.15s;
}

.hero-edit svg {
  width: 1rem;
  height: 1rem;
}

.hero-edit:hover {
  background: rgb(255 255 255 / 0.2);
  border-color: rgb(255 255 255 / 0.35);
}

.hero-edit:focus-visible {
  outline: none;
  box-shadow: 0 0 0 4px rgb(255 255 255 / 0.3);
}

.hero-stats {
  position: relative;
  z-index: 1;
  display: flex;
  gap: 0.75rem;
  margin: 0;
}

.stat {
  min-width: 6.5rem;
  padding: 0.8rem 1.1rem;
  background: rgb(255 255 255 / 0.1);
  border: 1px solid rgb(255 255 255 / 0.16);
  border-radius: 14px;
  backdrop-filter: blur(6px);
}

.stat dt {
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgb(255 255 255 / 0.7);
}

.stat-budget {
  background: rgb(255 255 255 / 0.16);
  border-color: rgb(243 201 179 / 0.45);
}

.stat dd {
  margin: 0.15rem 0 0;
  font-family: var(--font-display);
  font-size: 1.9rem;
  font-weight: 700;
  line-height: 1.1;
}

.layout {
  display: grid;
  grid-template-columns: minmax(320px, 380px) minmax(0, 1fr);
  gap: 2rem;
  align-items: start;
}

.form-panel {
  position: sticky;
  top: 1.5rem;
  padding: 1.6rem;
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
}

.form-panel .section-title {
  margin: 0.2rem 0 1.25rem;
  overflow-wrap: anywhere;
}

.form-panel.editing {
  border-color: rgb(31 92 86 / 0.45);
  box-shadow:
    0 0 0 4px var(--color-primary-ring),
    var(--shadow);
}

.form-alert {
  margin-top: 1.1rem;
}

.schedule-panel {
  min-width: 0;
}

.schedule-header {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.25rem 1rem;
  margin-bottom: 1rem;
}

.rates-note {
  font-size: 0.8rem;
  color: var(--color-subtle);
}

.rates-note.warning {
  color: var(--color-accent);
  font-weight: 600;
}

.schedule-title {
  margin: 0.4rem 0 0;
}

.loading {
  color: var(--color-muted);
}

@media (max-width: 900px) {
  .layout {
    grid-template-columns: 1fr;
  }

  .form-panel {
    position: static;
  }
}

@media (max-width: 640px) {
  .hero {
    flex-direction: column;
    align-items: flex-start;
    gap: 1.25rem;
  }

  .stat {
    min-width: 0;
    flex: 1;
  }

  .hero-stats {
    width: 100%;
  }
}
</style>

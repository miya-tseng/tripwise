<script setup lang="ts">
import { computed, inject, nextTick, ref, watch } from 'vue'
import { RouterLink, routerKey } from 'vue-router'
import { storeToRefs } from 'pinia'
import ActivityDialog from '@/components/ActivityDialog.vue'
import type { ActivityPrefill } from '@/components/ActivityForm.vue'
import BoardView, { type MoveRequest } from '@/components/BoardView.vue'
import ScheduleDays from '@/components/ScheduleDays.vue'
import StayDialog from '@/components/StayDialog.vue'
import SuggestionsPanel from '@/components/SuggestionsPanel.vue'
import SelectMenu, { type SelectOption } from '@/components/SelectMenu.vue'
import TableView from '@/components/TableView.vue'
import TripEditDialog from '@/components/TripEditDialog.vue'
import { useScheduleStore } from '@/stores/schedule'
import type { Activity, ActivityCategory, BookingStatus, SaveActivityInput } from '@/types/activity'
import type { GeoPoint } from '@/types/place'
import type { SaveStayInput, Stay } from '@/types/stay'
import type { Suggestion } from '@/types/suggestion'
import type { UpdateTripInput } from '@/types/trip'
import { CATEGORIES } from '@/utils/activityDetails'
import { formatDateRange } from '@/utils/date'
import {
  formatCurrency,
  formatTotals,
  formatTwd,
  totalsByCurrency,
  tripCosts,
  tripTotalInTwd,
} from '@/utils/money'
import { lastUsedCurrency, lastUsedView, rememberCurrency, rememberView } from '@/utils/preferences'
import { filterDays, parseView, type ScheduleView } from '@/utils/activityTable'
import { slotAfter } from '@/utils/schedule'
import { firstFreeNight, nextCheckInAfter, nightsLabel, stayRangeLabel } from '@/utils/stays'
import { addDays } from '@/utils/date'
import { todayIso } from '@/utils/trips'
import { BOOKING_STATUSES, bookingStatusMeta } from '@/utils/activityDetails'

const props = withDefaults(
  defineProps<{
    id: string
    day?: string
    view?: string
    today?: string
  }>(),
  { day: undefined, view: undefined, today: () => todayIso() },
)

const store = useScheduleStore()
const {
  trip,
  days,
  stays,
  stayError,
  loading,
  loadError,
  error,
  deletingId,
  editTripError,
  ratesAvailable,
  ratesUpdatedAt,
  exchangeRates,
  suggestionsFor,
  suggestions,
  suggestionsLoading,
  suggestionsError,
} = storeToRefs(store)

const dialogOpen = ref(false)
const editing = ref<Activity | null>(null)
const addingDate = ref<string | null>(null)
const prefill = ref<ActivityPrefill | null>(null)
const formVersion = ref(0)
const tripEditorOpen = ref(false)
const router = inject(routerKey, null)
const scheduleView = ref<ScheduleView>(initialView(props.id))
const categoryFilter = ref<ActivityCategory | null>(null)
const bookingFilter = ref<BookingStatus | null>(null)
const movingId = ref<string | null>(null)
const defaultCurrency = ref(lastUsedCurrency(props.id))
const stayDialogOpen = ref(false)
const editingStay = ref<Stay | null>(null)
const stayCheckIn = ref<string | null>(null)
const stayCheckOut = ref<string | null>(null)

const dates = computed(() => days.value.map((day) => day.date))
const formKey = computed(() => editing.value?.id ?? `new-${formVersion.value}`)
const activityCount = computed(() =>
  days.value.reduce((total, day) => total + day.activities.length, 0),
)
const categoriesInTrip = computed(() => {
  const used = new Set(
    days.value.flatMap((day) => day.activities.map((activity) => activity.category)),
  )
  return CATEGORIES.filter((category) => used.has(category.value))
})
const visibleDays = computed(() =>
  filterDays(days.value, { category: categoryFilter.value, bookingStatus: bookingFilter.value }),
)
const bookingStatusesInTrip = computed(
  () => new Set(days.value.flatMap((day) => day.activities.map((a) => a.bookingStatus))),
)
const bookingFilterOptions = computed<SelectOption[]>(() => [
  { value: '', label: 'All bookings' },
  ...BOOKING_STATUSES.filter((status) => bookingStatusesInTrip.value.has(status.value)).map(
    (status) => ({ value: status.value, label: status.label }),
  ),
])
const bookingFilterValue = computed({
  get: () => bookingFilter.value ?? '',
  set: (value: string) => {
    bookingFilter.value = value === '' ? null : (value as BookingStatus)
  },
})
const showBookingFilter = computed(
  () => bookingStatusesInTrip.value.size > 1 || bookingFilter.value !== null,
)
const viewOptions: { value: ScheduleView; label: string }[] = [
  { value: 'timeline', label: 'Timeline' },
  { value: 'board', label: 'Board' },
  { value: 'table', label: 'Table' },
]
const searchNear = computed<GeoPoint | null>(() => {
  const places = days.value.flatMap((day) =>
    day.activities.flatMap((activity) => activity.place ?? []),
  )
  const last = places.at(-1) ?? trip.value?.destination
  return last ? { latitude: last.latitude, longitude: last.longitude } : null
})
const budgetInTwd = computed(() => tripTotalInTwd(days.value, stays.value))
const budget = computed(() =>
  budgetInTwd.value === null
    ? formatTotals(totalsByCurrency(tripCosts(days.value, stays.value)))
    : formatTwd(budgetInTwd.value),
)
const nightsWithoutStay = computed(() => days.value.filter((day) => !day.stay).length)
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

function initialView(tripId: string): ScheduleView {
  return parseView(props.view) ?? parseView(lastUsedView(tripId)) ?? 'timeline'
}

function setView(view: ScheduleView) {
  scheduleView.value = view
  rememberView(props.id, view)

  const query = { ...router?.currentRoute.value.query }
  delete query.day

  if (view === 'timeline') {
    delete query.view
  } else {
    query.view = view
  }

  router?.replace({ query })
}

watch(
  () => props.view,
  (view) => {
    const parsed = parseView(view)

    if (parsed) {
      scheduleView.value = parsed
    }
  },
)

watch(
  () => props.id,
  (id, previousId) => {
    if (previousId !== undefined) {
      scheduleView.value = initialView(id)
    }
    closeDialog()
    defaultCurrency.value = lastUsedCurrency(id)
    store.load(id)
  },
  { immediate: true },
)

watch(
  () => loading.value || !trip.value,
  async (notReady) => {
    const dayNumber = props.day ? Number(props.day) : dates.value.indexOf(props.today) + 1

    const minimumDay = props.day ? 1 : 2

    if (notReady || !Number.isInteger(dayNumber) || dayNumber < minimumDay) {
      return
    }

    await nextTick()
    document
      .querySelectorAll('[data-testid="schedule-day"]')
      [dayNumber - 1]?.scrollIntoView?.({ block: 'start', behavior: 'smooth' })
  },
  { immediate: true },
)

function openDialog(options: { activity?: Activity; date?: string; prefill?: ActivityPrefill }) {
  store.clearError()
  editing.value = options.activity ?? null
  addingDate.value = options.date ?? null
  prefill.value = options.prefill ?? null
  formVersion.value++
  dialogOpen.value = true
}

function closeDialog() {
  dialogOpen.value = false
  editing.value = null
  prefill.value = null
  addingDate.value = null
  store.clearError()
}

function addToDay(date?: string) {
  openDialog({ date: date ?? dates.value[0] })
}

function startEditing(activity: Activity) {
  openDialog({ activity })
}

async function save(input: SaveActivityInput): Promise<boolean> {
  const saved = editing.value ? await store.update(editing.value.id, input) : await store.add(input)

  if (!saved) {
    return false
  }

  if (prefill.value && input.placeId === prefill.value.place.placeId) {
    store.dropSuggestion(input.placeId)
  }

  if (input.cost > 0) {
    rememberCurrency(props.id, input.currency)
    defaultCurrency.value = input.currency
  }

  return true
}

async function moveActivity(request: MoveRequest) {
  movingId.value = request.activityId
  await store.move(request.activityId, { date: request.date, startTime: request.startTime })
  movingId.value = null
}

async function removeActivity(id: string) {
  if (suggestionsFor.value?.id === id) {
    store.closeSuggestions()
  }

  await store.remove(id)
}

function showSuggestions(activity: Activity) {
  store.loadSuggestions(activity)
}

function addSuggestion(suggestion: Suggestion) {
  const anchor = suggestionsFor.value

  if (!anchor) {
    return
  }

  openDialog({
    prefill: {
      place: {
        placeId: suggestion.place.placeId,
        name: suggestion.place.name,
        description: suggestion.place.address,
      },
      date: anchor.date,
      ...slotAfter(anchor.endTime, suggestion.suggestedMinutes),
    },
  })
}

function openTripEditor() {
  store.clearEditTripError()
  tripEditorOpen.value = true
}

function closeTripEditor() {
  tripEditorOpen.value = false
}

function openStayDialog(options: { stay?: Stay; checkIn?: string }) {
  store.clearStayError()
  editingStay.value = options.stay ?? null
  const checkIn =
    options.checkIn ?? firstFreeNight(dates.value, stays.value) ?? dates.value[0] ?? null
  stayCheckIn.value = checkIn
  stayCheckOut.value = checkIn
    ? (nextCheckInAfter(checkIn, stays.value) ?? addDays(checkIn, 1))
    : null
  stayDialogOpen.value = true
}

function closeStayDialog() {
  stayDialogOpen.value = false
  editingStay.value = null
  store.clearStayError()
}

async function saveStay(input: SaveStayInput): Promise<boolean> {
  const saved = await store.saveStay(editingStay.value?.id ?? null, input)

  if (saved && input.cost > 0) {
    rememberCurrency(props.id, input.currency)
    defaultCurrency.value = input.currency
  }

  return saved
}

async function saveTrip(input: UpdateTripInput): Promise<boolean> {
  return store.editTrip(input)
}
</script>

<template>
  <main class="page">
    <div v-if="loading" class="status" aria-busy="true">
      <p class="loading">Loading...</p>
    </div>

    <div v-else-if="loadError" class="status">
      <RouterLink to="/" class="back-link" data-testid="back-link">← All trips</RouterLink>
      <div role="alert" class="alert" data-testid="load-error">{{ loadError }}</div>
    </div>

    <template v-else-if="trip">
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
                class="btn btn-secondary btn-small"
                data-testid="edit-trip"
                @click="openTripEditor"
              >
                Edit trip
              </button>
              <button
                type="button"
                class="btn btn-primary btn-small"
                data-testid="add-activity"
                @click="addToDay()"
              >
                + Add activity
              </button>
            </div>
          </div>

          <div class="bar-bottom">
            <div class="view-toggle" role="group" aria-label="Schedule view">
              <button
                v-for="option in viewOptions"
                :key="option.value"
                type="button"
                :aria-pressed="scheduleView === option.value"
                :data-testid="`view-${option.value}`"
                @click="setView(option.value)"
              >
                {{ option.label }}
              </button>
            </div>
            <div v-if="showBookingFilter" class="booking-filter" data-testid="booking-filter">
              <SelectMenu
                v-model="bookingFilterValue"
                :options="bookingFilterOptions"
                name="booking-filter"
              />
            </div>
            <div
              v-if="categoriesInTrip.length > 1"
              class="category-filter"
              role="group"
              aria-label="Filter by category"
            >
              <button
                type="button"
                class="filter-chip"
                :aria-pressed="categoryFilter === null"
                data-testid="filter-all"
                @click="categoryFilter = null"
              >
                All
              </button>
              <button
                v-for="category in categoriesInTrip"
                :key="category.value"
                type="button"
                class="filter-chip"
                :class="`category-${category.value.toLowerCase()}`"
                :aria-pressed="categoryFilter === category.value"
                :data-testid="`filter-${category.value}`"
                @click="categoryFilter = category.value"
              >
                {{ category.icon }} {{ category.label }}
              </button>
            </div>
          </div>
        </div>
      </header>

      <section class="itinerary" aria-label="Itinerary">
        <p
          v-if="ratesNote"
          class="rates-note"
          :class="{ warning: !ratesAvailable }"
          data-testid="rates-note"
        >
          {{ ratesNote }}
        </p>
        <div
          v-if="error && !dialogOpen"
          role="alert"
          class="alert page-alert"
          data-testid="schedule-error"
        >
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

        <section class="stays" aria-label="Stays" data-testid="stays-section">
          <div class="stays-head">
            <h2 class="stays-title">Stays</h2>
            <span v-if="nightsWithoutStay > 0" class="stays-hint" data-testid="nights-without-stay">
              {{ nightsLabel(nightsWithoutStay) }} without a stay
            </span>
            <button
              type="button"
              class="btn btn-secondary btn-small"
              data-testid="add-stay"
              @click="openStayDialog({})"
            >
              + Add stay
            </button>
          </div>
          <ul v-if="stays.length > 0" class="stay-cards">
            <li v-for="stay in stays" :key="stay.id">
              <button
                type="button"
                class="stay-card"
                data-testid="stay-card"
                @click="openStayDialog({ stay })"
              >
                <span class="stay-card-name" data-testid="stay-card-name">🛏 {{ stay.name }}</span>
                <span class="stay-card-dates">
                  {{ stayRangeLabel(stay) }} · {{ nightsLabel(stay.nights) }}
                </span>
                <span class="stay-card-foot">
                  <span v-if="stay.cost > 0" data-testid="stay-card-cost">{{
                    formatCurrency(stay.cost, stay.currency)
                  }}</span>
                  <span
                    v-if="stay.bookingStatus !== 'NotNeeded'"
                    class="booking-badge"
                    :class="`tone-${bookingStatusMeta(stay.bookingStatus).tone}`"
                  >
                    {{ bookingStatusMeta(stay.bookingStatus).label }}
                  </span>
                </span>
              </button>
            </li>
          </ul>
        </section>

        <ScheduleDays
          v-if="scheduleView === 'timeline'"
          :days="visibleDays"
          :today="today"
          :editing-id="editing?.id ?? null"
          :deleting-id="deletingId"
          @add="addToDay"
          @edit="startEditing"
          @delete="removeActivity"
          @suggest="showSuggestions"
          @add-stay="(date) => openStayDialog({ checkIn: date })"
          @edit-stay="(stay) => openStayDialog({ stay })"
        />
        <TableView v-else-if="scheduleView === 'table'" :days="visibleDays" @edit="startEditing" />
        <BoardView
          v-else
          :days="visibleDays"
          :today="today"
          :moving-id="movingId"
          @add="addToDay"
          @move="moveActivity"
          @edit="startEditing"
        />
      </section>

      <Transition name="sheet">
        <aside v-if="suggestionsFor" class="suggestions-sheet" data-testid="suggestions-sheet">
          <SuggestionsPanel
            :anchor="suggestionsFor"
            :suggestions="suggestions"
            :loading="suggestionsLoading"
            :error="suggestionsError"
            @close="store.closeSuggestions"
            @add="addSuggestion"
          />
        </aside>
      </Transition>

      <ActivityDialog
        :open="dialogOpen"
        :form-key="formKey"
        :dates="dates"
        :activity="editing"
        :prefill="prefill"
        :initial-date="addingDate"
        :submit="save"
        :rates="exchangeRates"
        :default-currency="defaultCurrency"
        :near="searchNear"
        :error="error"
        @close="closeDialog"
        @dismiss-error="store.clearError"
      />

      <StayDialog
        :open="stayDialogOpen"
        :dates="dates"
        :stay="editingStay"
        :initial-check-in="stayCheckIn"
        :initial-check-out="stayCheckOut"
        :submit="saveStay"
        :remove="store.removeStay"
        :rates="exchangeRates"
        :default-currency="defaultCurrency"
        :near="searchNear"
        :error="stayError"
        @close="closeStayDialog"
        @dismiss-error="store.clearStayError"
      />

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
.stays {
  margin-bottom: 1.25rem;
}

.stays-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.6rem;
}

.stays-title {
  font-size: 1rem;
  font-weight: 700;
}

.stays-hint {
  font-size: 0.82rem;
  color: var(--color-muted);
}

.stays-head .btn {
  margin-left: auto;
}

.stay-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
  gap: 0.6rem;
  padding: 0;
  list-style: none;
}

.stay-card {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  width: 100%;
  padding: 0.75rem 0.9rem;
  font: inherit;
  text-align: left;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  cursor: pointer;
}

.stay-card:hover {
  border-color: var(--color-primary);
}

.stay-card-name {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.stay-card-dates {
  font-size: 0.82rem;
  color: var(--color-muted);
}

.stay-card-foot {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  font-variant-numeric: tabular-nums;
}

.page {
  min-height: 100vh;
  padding-bottom: 4.5rem;
}

.status {
  max-width: 960px;
  margin: 0 auto;
  padding: 2rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

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

.status .back-link {
  width: auto;
  height: auto;
  justify-self: start;
  font-weight: 600;
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
  flex-wrap: wrap;
  align-items: center;
  gap: 0.6rem 1rem;
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

.booking-filter {
  width: 9.5rem;
}

.booking-filter :deep(.select-trigger) {
  height: 2rem;
  padding: 0 0.75rem;
  font-size: 0.8rem;
  font-weight: 600;
  border-radius: 999px;
  box-shadow: none;
}

.category-filter {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.filter-chip {
  padding: 0.25rem 0.7rem;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.filter-chip[aria-pressed='true'] {
  color: var(--category-color, var(--color-text));
  background: var(--category-soft, var(--color-surface-muted));
  border-color: var(--category-color, var(--color-border-strong));
}

.itinerary {
  max-width: 1100px;
  margin: 0 auto;
  padding: 1.25rem clamp(1rem, 3vw, 2rem) 0;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.rates-note {
  align-self: flex-end;
  font-size: 0.78rem;
  color: var(--color-subtle);
}

.rates-note.warning {
  font-weight: 600;
  color: var(--color-accent);
}

.page-alert {
  position: sticky;
  top: 7.5rem;
  z-index: 20;
}

.suggestions-sheet {
  position: fixed;
  z-index: 60;
  top: 0;
  right: 0;
  bottom: 0;
  width: min(400px, 100vw);
  padding: 1rem;
  overflow-y: auto;
  background: rgb(245 242 236 / 0.96);
  border-left: 1px solid var(--color-border);
  box-shadow: var(--shadow-lg);
}

.sheet-enter-active,
.sheet-leave-active {
  transition: transform 0.22s ease;
}

.sheet-enter-from,
.sheet-leave-to {
  transform: translateX(100%);
}

.loading {
  color: var(--color-muted);
}

@media (max-width: 720px) {
  .bar-inner {
    padding: 0.6rem 0.9rem 0.55rem;
    gap: 0.45rem;
  }

  .bar-top {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    grid-template-areas:
      'back title budget'
      'actions actions actions';
    gap: 0.4rem 0.6rem;
  }

  .back-link {
    grid-area: back;
  }

  .trip-title {
    grid-area: title;
  }

  h1 {
    font-size: 1.2rem;
  }

  .trip-meta {
    font-size: 0.75rem;
  }

  .budget {
    grid-area: budget;
  }

  .budget-value {
    font-size: 1rem;
  }

  .bar-actions {
    grid-area: actions;
  }

  .bar-actions .btn {
    flex: 1;
  }

  .bar-bottom {
    flex-wrap: nowrap;
    overflow-x: auto;
    scrollbar-width: none;
  }

  .category-filter {
    flex-wrap: nowrap;
  }

  .filter-chip,
  .view-toggle {
    flex-shrink: 0;
  }

  .rates-note {
    align-self: stretch;
    font-size: 0.72rem;
  }
}
</style>

<script setup lang="ts">
import { computed, ref, toRef, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { storeToRefs } from 'pinia'
import ActivityDialog from '@/components/ActivityDialog.vue'
import type { ActivityPrefill } from '@/components/ActivityForm.vue'
import AddFab from '@/components/AddFab.vue'
import BoardView, { type MoveRequest } from '@/components/BoardView.vue'
import DayPager from '@/components/DayPager.vue'
import ScheduleDays from '@/components/ScheduleDays.vue'
import StayDialog from '@/components/StayDialog.vue'
import StaysSummary from '@/components/StaysSummary.vue'
import SuggestionsPanel from '@/components/SuggestionsPanel.vue'
import TableView from '@/components/TableView.vue'
import TripEditDialog from '@/components/TripEditDialog.vue'
import TripScheduleHeader from '@/components/TripScheduleHeader.vue'
import { useActiveDay } from '@/composables/useActiveDay'
import { useActivityFilters } from '@/composables/useActivityFilters'
import { useElementHeight } from '@/composables/useElementHeight'
import { useMediaQuery } from '@/composables/useMediaQuery'
import { useScheduleView } from '@/composables/useScheduleView'
import { useScheduleStore } from '@/stores/schedule'
import type { Activity, SaveActivityInput } from '@/types/activity'
import type { GeoPoint } from '@/types/place'
import type { SaveStayInput, Stay } from '@/types/stay'
import type { Suggestion } from '@/types/suggestion'
import type { UpdateTripInput } from '@/types/trip'
import { addDays } from '@/utils/date'
import { formatTotals, formatTwd, totalsByCurrency, tripCosts, tripTotalInTwd } from '@/utils/money'
import { lastUsedCurrency, rememberCurrency } from '@/utils/preferences'
import { slotAfter } from '@/utils/schedule'
import { extendedByOneNight, firstFreeNight, nextCheckInAfter } from '@/utils/stays'
import { todayIso } from '@/utils/trips'

const props = withDefaults(
  defineProps<{
    id: string
    day?: string
    view?: string
    today?: string
  }>(),
  { day: undefined, view: undefined, today: () => todayIso() },
)

const PHONE_WIDTH = '(max-width: 720px)'
const RATES_DATE = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
})

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

const tripId = toRef(props, 'id')
const header = ref<InstanceType<typeof TripScheduleHeader> | null>(null)
const headerOffset = useElementHeight(computed(() => header.value?.$el as HTMLElement | undefined))
const paged = useMediaQuery(PHONE_WIDTH)
const dates = computed(() => days.value.map((day) => day.date))

const { scheduleView, setView } = useScheduleView(tripId, toRef(props, 'view'))
const filters = useActivityFilters(days)
const { activeDayIndex, currentDayDate, jumpToDay, showDay, onTouchStart, onTouchEnd } =
  useActiveDay({
    dates,
    paged,
    headerOffset,
    ready: computed(() => !loading.value && trip.value !== null),
    linkedDay: toRef(props, 'day'),
    today: toRef(props, 'today'),
  })

const dialogOpen = ref(false)
const editing = ref<Activity | null>(null)
const addingDate = ref<string | null>(null)
const prefill = ref<ActivityPrefill | null>(null)
const formVersion = ref(0)
const tripEditorOpen = ref(false)
const movingId = ref<string | null>(null)
const defaultCurrency = ref(lastUsedCurrency(props.id))
const stayDialogOpen = ref(false)
const editingStay = ref<Stay | null>(null)
const stayCheckIn = ref<string | null>(null)
const stayCheckOut = ref<string | null>(null)

const formKey = computed(() => editing.value?.id ?? `new-${formVersion.value}`)
const activityCount = computed(() =>
  days.value.reduce((total, day) => total + day.activities.length, 0),
)
const searchNear = computed<GeoPoint | null>(() => {
  const places = days.value.flatMap((day) =>
    day.activities.flatMap((activity) => activity.place ?? []),
  )
  const last = places.at(-1) ?? trip.value?.destination
  return last ? { latitude: last.latitude, longitude: last.longitude } : null
})
const budget = computed(() => {
  const totalInTwd = tripTotalInTwd(days.value, stays.value)
  return totalInTwd === null
    ? formatTotals(totalsByCurrency(tripCosts(days.value, stays.value)))
    : formatTwd(totalInTwd)
})
const nightsWithoutStay = computed(() => days.value.filter((day) => !day.stay).length)
const ratesNote = computed(() => {
  if (!ratesAvailable.value) {
    return 'Exchange rates are unavailable right now, so totals are shown per currency.'
  }

  return ratesUpdatedAt.value
    ? `Totals in TWD · Rates by Exchange Rate API, updated ${RATES_DATE.format(new Date(ratesUpdatedAt.value))}`
    : ''
})

watch(
  tripId,
  (id) => {
    closeDialog()
    defaultCurrency.value = lastUsedCurrency(id)
    store.load(id)
  },
  { immediate: true },
)

function rememberCurrencyOf(cost: number, currency: string) {
  if (cost > 0) {
    rememberCurrency(props.id, currency)
    defaultCurrency.value = currency
  }
}

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

  rememberCurrencyOf(input.cost, input.currency)
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

async function removeActivityFromDialog(id: string): Promise<boolean> {
  await removeActivity(id)
  return !error.value
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

function showStayDialog(stay: Stay | null, checkIn: string | null, checkOut: string | null) {
  editingStay.value = stay
  stayCheckIn.value = checkIn
  stayCheckOut.value = checkOut
  stayDialogOpen.value = true
}

function openStayDialog(options: { stay?: Stay; checkIn?: string }) {
  store.clearStayError()
  const checkIn =
    options.checkIn ?? firstFreeNight(dates.value, stays.value) ?? dates.value[0] ?? null
  const checkOut = checkIn ? (nextCheckInAfter(checkIn, stays.value) ?? addDays(checkIn, 1)) : null
  showStayDialog(options.stay ?? null, checkIn, checkOut)
}

async function extendStay(stay: Stay) {
  const extended = await store.saveStay(stay.id, extendedByOneNight(stay))

  if (!extended) {
    showStayDialog(stay, stay.checkIn, stay.checkOut)
  }
}

function closeStayDialog() {
  stayDialogOpen.value = false
  editingStay.value = null
  store.clearStayError()
}

async function saveStay(input: SaveStayInput): Promise<boolean> {
  const saved = await store.saveStay(editingStay.value?.id ?? null, input)

  if (saved) {
    rememberCurrencyOf(input.cost, input.currency)
  }

  return saved
}

async function saveTrip(input: UpdateTripInput): Promise<boolean> {
  return store.editTrip(input)
}
</script>

<template>
  <main class="page" :style="{ '--header-offset': `${headerOffset}px` }">
    <div v-if="loading" class="status" aria-busy="true">
      <p class="loading">Loading...</p>
    </div>

    <div v-else-if="loadError" class="status">
      <RouterLink to="/" class="home-link" data-testid="back-link">← All trips</RouterLink>
      <div role="alert" class="alert" data-testid="load-error">{{ loadError }}</div>
    </div>

    <template v-else-if="trip">
      <TripScheduleHeader
        ref="header"
        v-model:booking="filters.bookingValue.value"
        v-model:category="filters.categoryValue.value"
        :trip="trip"
        :activity-count="activityCount"
        :budget="budget"
        :rates-note="ratesNote"
        :schedule-view="scheduleView"
        :dates="dates"
        :active-day-index="activeDayIndex"
        :today="today"
        :booking-options="filters.bookingOptions.value"
        :categories="filters.categoriesInTrip.value"
        @edit-trip="openTripEditor"
        @add-activity="addToDay()"
        @change-view="setView"
        @jump="jumpToDay"
      />

      <section
        class="itinerary"
        aria-label="Itinerary"
        @touchstart.passive="onTouchStart"
        @touchend.passive="onTouchEnd"
      >
        <p
          v-if="!ratesAvailable"
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

        <StaysSummary
          :stays="stays"
          :nights-without-stay="nightsWithoutStay"
          @add="openStayDialog({})"
          @edit="(stay) => openStayDialog({ stay })"
        />

        <template v-if="scheduleView === 'timeline'">
          <ScheduleDays
            :days="filters.visibleDays.value"
            :today="today"
            :active-date="paged ? currentDayDate : null"
            :editing-id="editing?.id ?? null"
            :deleting-id="deletingId"
            @add="addToDay"
            @edit="startEditing"
            @delete="removeActivity"
            @suggest="showSuggestions"
            @add-stay="(date) => openStayDialog({ checkIn: date })"
            @edit-stay="(stay) => openStayDialog({ stay })"
            @extend-stay="extendStay"
          />
          <DayPager
            v-if="paged && dates.length > 1"
            :active-index="activeDayIndex"
            :day-count="dates.length"
            @step="showDay"
          />
        </template>
        <TableView
          v-else-if="scheduleView === 'table'"
          :days="filters.visibleDays.value"
          @edit="startEditing"
        />
        <BoardView
          v-else
          :days="filters.visibleDays.value"
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

      <AddFab
        @add-activity="addToDay(currentDayDate)"
        @add-stay="openStayDialog({ checkIn: currentDayDate })"
      />

      <ActivityDialog
        :open="dialogOpen"
        :form-key="formKey"
        :remove="removeActivityFromDialog"
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
        :stays="stays"
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

.home-link {
  justify-self: start;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
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
</style>

<script setup lang="ts">
import { computed, ref, useId } from 'vue'
import PlaceSearch from '@/components/PlaceSearch.vue'
import SelectMenu, { type SelectOption } from '@/components/SelectMenu.vue'
import TimePicker from '@/components/TimePicker.vue'
import {
  MAX_ACTIVITY_NAME_LENGTH,
  MAX_CONFIRMATION_CODE_LENGTH,
  MAX_NOTES_LENGTH,
  type Activity,
  type ActivityCategory,
  type BookingStatus,
  type SaveActivityInput,
} from '@/types/activity'
import { formatDayLabel } from '@/utils/date'
import { BOOKING_STATUSES, CATEGORIES, linkError } from '@/utils/activityDetails'
import { COMMON_CURRENCIES, HOME_CURRENCY, type ExchangeRates } from '@/types/currency'
import type { GeoPoint, PlaceSuggestion } from '@/types/place'
import { currencyName, formatTwd, parseCost, toTwd } from '@/utils/money'
import { formatTime } from '@/utils/schedule'

const props = withDefaults(
  defineProps<{
    dates: string[]
    activity?: Activity | null
    submit: (input: SaveActivityInput) => Promise<boolean>
    rates?: ExchangeRates | null
    defaultCurrency?: string
    near?: GeoPoint | null
    prefill?: ActivityPrefill | null
  }>(),
  {
    activity: null,
    rates: null,
    defaultCurrency: HOME_CURRENCY,
    near: null,
    prefill: null,
  },
)

const emit = defineEmits<{
  cancel: []
}>()

export interface ActivityPrefill {
  place: PlaceSuggestion
  date: string
  startTime: string
  endTime: string
}

const isEditing = computed(() => props.activity !== null)
const prefilled = !props.activity ? props.prefill : null

const activityFieldId = useId()

function newSessionToken(): string {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

const name = ref(props.activity?.name ?? '')
const mode = ref<'place' | 'custom'>(props.activity && !props.activity.place ? 'custom' : 'place')
const sessionToken = ref(newSessionToken())
const selectedPlace = ref<PlaceSuggestion | null>(
  props.activity?.place
    ? {
        placeId: props.activity.place.placeId,
        name: props.activity.name,
        description: props.activity.place.address,
      }
    : (prefilled?.place ?? null),
)
const date = ref(props.activity?.date ?? prefilled?.date ?? props.dates[0] ?? '')
const startTime = ref(
  props.activity ? formatTime(props.activity.startTime) : (prefilled?.startTime ?? ''),
)
const endTime = ref(
  props.activity ? formatTime(props.activity.endTime) : (prefilled?.endTime ?? ''),
)
const cost = ref(props.activity && props.activity.cost > 0 ? props.activity.cost.toFixed(2) : '')
const currency = ref(props.activity?.currency ?? props.defaultCurrency)
const category = ref<ActivityCategory>(props.activity?.category ?? 'Other')
const bookingStatus = ref<BookingStatus>(props.activity?.bookingStatus ?? 'NotNeeded')
const notes = ref(props.activity?.notes ?? '')
const confirmationCode = ref(props.activity?.confirmationCode ?? '')
const link = ref(props.activity?.link ?? '')
const detailsOpen = ref(
  Boolean(props.activity?.notes || props.activity?.confirmationCode || props.activity?.link),
)
const bookingOptions = BOOKING_STATUSES.map((status) => ({
  value: status.value,
  label: status.label,
}))
const linkProblem = computed(() => linkError(link.value))
const submitting = ref(false)

const dateOptions = computed<SelectOption[]>(() =>
  props.dates.map((value, index) => ({
    value,
    label: `Day ${index + 1}`,
    hint: formatDayLabel(value),
  })),
)

const timesAreFilled = computed(() => startTime.value !== '' && endTime.value !== '')
const endsAfterStart = computed(() => endTime.value > startTime.value)
const timeError = computed(() =>
  timesAreFilled.value && !endsAfterStart.value ? 'End time must be later than start time.' : '',
)
const currencyOptions = computed<SelectOption[]>(() => {
  const available = props.rates?.available ? new Set(props.rates.currencies) : null
  const codes = COMMON_CURRENCIES.filter((code) => !available || available.has(code))
  const withCurrent = codes.includes(currency.value as (typeof COMMON_CURRENCIES)[number])
    ? codes
    : [...codes, currency.value]

  return withCurrent.map((code) => ({ value: code, label: code, hint: currencyName(code) }))
})

const parsedCost = computed(() => parseCost(cost.value))
const costPreview = computed(() => {
  if (!parsedCost.value.ok || parsedCost.value.value === 0 || currency.value === HOME_CURRENCY) {
    return ''
  }

  const converted = toTwd(parsedCost.value.value, currency.value, props.rates)
  return converted === null ? 'TWD amount unavailable' : `≈ ${formatTwd(converted)}`
})
const costError = computed(() => (parsedCost.value.ok ? '' : parsedCost.value.error))
const hasActivity = computed(() =>
  mode.value === 'place' ? selectedPlace.value !== null : name.value.trim() !== '',
)

function useCustomActivity() {
  mode.value = 'custom'
  if (selectedPlace.value && name.value.trim() === '') {
    name.value = selectedPlace.value.name
  }
}

function usePlaceSearch() {
  mode.value = 'place'
}

const canSubmit = computed(
  () =>
    hasActivity.value &&
    date.value !== '' &&
    timesAreFilled.value &&
    endsAfterStart.value &&
    parsedCost.value.ok &&
    linkProblem.value === '',
)

async function onSubmit() {
  if (!canSubmit.value || !parsedCost.value.ok) {
    return
  }

  submitting.value = true

  const place = mode.value === 'place' ? selectedPlace.value : null
  const saved = await props.submit({
    name: place ? place.name : name.value,
    ...(place ? { placeId: place.placeId, placeSessionToken: sessionToken.value } : {}),
    date: date.value,
    startTime: startTime.value,
    endTime: endTime.value,
    cost: parsedCost.value.value,
    currency: currency.value,
    category: category.value,
    bookingStatus: bookingStatus.value,
    notes: notes.value.trim(),
    confirmationCode: confirmationCode.value.trim(),
    link: link.value.trim() || null,
  })

  submitting.value = false

  if (saved && place) {
    sessionToken.value = newSessionToken()
  }

  if (saved && !isEditing.value) {
    name.value = ''
    selectedPlace.value = null
    startTime.value = ''
    endTime.value = ''
    cost.value = ''
    category.value = 'Other'
    bookingStatus.value = 'NotNeeded'
    notes.value = ''
    confirmationCode.value = ''
    link.value = ''
    detailsOpen.value = false
  }
}
</script>

<template>
  <form class="activity-form" data-testid="activity-form" @submit.prevent="onSubmit">
    <div class="field">
      <label class="field-label" :for="activityFieldId">Activity</label>
      <PlaceSearch
        v-if="mode === 'place'"
        v-model="selectedPlace"
        :input-id="activityFieldId"
        :session-token="sessionToken"
        :near="near"
      />
      <input
        v-else
        :id="activityFieldId"
        v-model="name"
        class="text-input"
        name="name"
        type="text"
        placeholder="e.g. Free time"
        autocomplete="off"
        required
        :maxlength="MAX_ACTIVITY_NAME_LENGTH"
      />
      <button
        v-if="mode === 'place'"
        type="button"
        class="mode-switch"
        data-testid="use-custom-activity"
        @click="useCustomActivity"
      >
        Not a place? Add a custom activity
      </button>
      <button
        v-else
        type="button"
        class="mode-switch"
        data-testid="use-place-search"
        @click="usePlaceSearch"
      >
        Search a real place instead
      </button>
    </div>

    <div class="field">
      <span class="field-label">Day</span>
      <SelectMenu v-model="date" name="date" :options="dateOptions" data-testid="day-select" />
    </div>

    <div class="time-row">
      <div class="field">
        <span class="field-label">Start</span>
        <TimePicker v-model="startTime" name="startTime" label="Start" />
      </div>
      <span class="time-separator" aria-hidden="true">
        <svg viewBox="0 0 20 20">
          <path
            d="M4 10h11m-4-4 4 4-4 4"
            fill="none"
            stroke="currentColor"
            stroke-width="1.6"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
      </span>
      <div class="field">
        <span class="field-label">End</span>
        <TimePicker v-model="endTime" name="endTime" label="End" align="end" />
      </div>
    </div>
    <p v-if="timeError" class="field-error" data-testid="time-error">{{ timeError }}</p>

    <div class="field">
      <span class="field-label">
        Cost
        <span class="field-optional">Optional</span>
      </span>
      <div class="cost-row" :class="{ invalid: costError }">
        <input
          v-model="cost"
          class="text-input cost-amount"
          name="cost"
          type="text"
          inputmode="decimal"
          placeholder="0.00"
          autocomplete="off"
          aria-label="Cost"
          :aria-invalid="costError !== ''"
        />
        <SelectMenu
          v-model="currency"
          class="cost-currency"
          align="end"
          name="currency"
          :options="currencyOptions"
          data-testid="currency-select"
        />
      </div>
      <span v-if="costPreview" class="cost-preview" data-testid="cost-preview">
        {{ costPreview }}
      </span>
    </div>
    <p v-if="costError" class="field-error" data-testid="cost-error">{{ costError }}</p>

    <div class="field">
      <span class="field-label">Category</span>
      <div class="category-chips" role="radiogroup" aria-label="Category">
        <button
          v-for="option in CATEGORIES"
          :key="option.value"
          type="button"
          role="radio"
          class="category-chip"
          :class="`category-${option.value.toLowerCase()}`"
          :aria-checked="category === option.value"
          :data-testid="`category-${option.value}`"
          @click="category = option.value"
        >
          <span aria-hidden="true">{{ option.icon }}</span>
          {{ option.label }}
        </button>
      </div>
    </div>

    <div class="field">
      <span class="field-label">Booking</span>
      <SelectMenu
        v-model="bookingStatus"
        name="bookingStatus"
        :options="bookingOptions"
        data-testid="booking-select"
      />
    </div>

    <div class="details" :class="{ open: detailsOpen }">
      <button
        type="button"
        class="details-toggle"
        :aria-expanded="detailsOpen"
        data-testid="toggle-details"
        @click="detailsOpen = !detailsOpen"
      >
        <span>Notes, confirmation &amp; link</span>
        <span aria-hidden="true">{{ detailsOpen ? '−' : '+' }}</span>
      </button>
      <div v-if="detailsOpen" class="details-body">
        <label class="field">
          <span class="field-label">Notes</span>
          <textarea
            v-model="notes"
            class="text-input notes-input"
            name="notes"
            rows="3"
            :maxlength="MAX_NOTES_LENGTH"
            placeholder="Opening hours, what to bring, tips…"
          />
        </label>
        <label class="field">
          <span class="field-label">Confirmation code</span>
          <input
            v-model="confirmationCode"
            class="text-input"
            name="confirmationCode"
            type="text"
            autocomplete="off"
            :maxlength="MAX_CONFIRMATION_CODE_LENGTH"
          />
        </label>
        <label class="field">
          <span class="field-label">Link</span>
          <input
            v-model="link"
            class="text-input"
            name="link"
            type="url"
            inputmode="url"
            autocomplete="off"
            placeholder="https://"
            :aria-invalid="linkProblem !== ''"
          />
        </label>
        <p v-if="linkProblem" class="field-error" data-testid="link-error">{{ linkProblem }}</p>
      </div>
    </div>

    <div class="actions">
      <button
        v-if="isEditing"
        type="button"
        class="btn btn-secondary"
        data-testid="cancel-edit"
        @click="emit('cancel')"
      >
        Cancel
      </button>
      <button type="submit" class="btn btn-primary" :disabled="submitting || !canSubmit">
        <template v-if="isEditing">{{ submitting ? 'Saving...' : 'Save' }}</template>
        <template v-else>{{ submitting ? 'Adding...' : 'Add' }}</template>
      </button>
    </div>
  </form>
</template>

<style scoped>
.activity-form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.time-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
  gap: 0.5rem;
  align-items: end;
}

.time-separator {
  display: grid;
  place-items: center;
  height: var(--control-height);
  color: var(--color-subtle);
}

.time-separator svg {
  width: 1.1rem;
  height: 1.1rem;
}

.category-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.category-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.35rem 0.7rem;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
  transition:
    background-color 0.15s,
    border-color 0.15s,
    color 0.15s;
}

.category-chip:hover {
  border-color: var(--color-border-strong);
}

.category-chip[aria-checked='true'] {
  color: var(--category-color, var(--color-primary));
  background: var(--category-soft, var(--color-primary-soft));
  border-color: var(--category-color, var(--color-primary));
}

.category-chip:focus-visible {
  outline: none;
  box-shadow: 0 0 0 4px var(--color-primary-ring);
}

.details {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-surface-muted);
}

.details-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 0.65rem 0.85rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-muted);
  background: none;
  border: none;
  cursor: pointer;
}

.details-body {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 0 0.85rem 0.85rem;
}

.notes-input {
  height: auto;
  padding: 0.6rem 0.9rem;
  resize: vertical;
  line-height: 1.5;
}

.mode-switch {
  align-self: flex-start;
  padding: 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-primary);
  background: none;
  border: none;
  cursor: pointer;
}

.mode-switch:hover {
  text-decoration: underline;
}

.field-error {
  margin-top: -0.6rem;
  font-size: 0.85rem;
  color: var(--color-danger);
}

.field-optional {
  font-weight: 500;
  color: var(--color-subtle);
}

.cost-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 7.5rem;
  gap: 0.5rem;
}

.cost-amount {
  font-variant-numeric: tabular-nums;
}

.cost-row.invalid .cost-amount {
  border-color: var(--color-danger);
}

.cost-row.invalid .cost-amount:focus {
  box-shadow: 0 0 0 4px rgb(179 65 47 / 0.15);
}

.cost-currency :deep(.select-menu) {
  width: 15rem;
}

.cost-currency :deep(.select-hint) {
  display: none;
}

.cost-preview {
  font-size: 0.85rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-primary);
}

.actions {
  display: flex;
  gap: 0.6rem;
  margin-top: 0.2rem;
}

.actions .btn {
  flex: 1;
}
</style>

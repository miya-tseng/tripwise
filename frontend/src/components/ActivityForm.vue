<script setup lang="ts">
import { computed, ref, useId } from 'vue'
import { newSessionToken } from '@/utils/sessionToken'
import CostField from '@/components/CostField.vue'
import PlaceNameField, { type PlaceNameMode } from '@/components/PlaceNameField.vue'
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
import { BOOKING_OPTIONS, CATEGORIES, linkError } from '@/utils/activityDetails'
import { HOME_CURRENCY, type ExchangeRates } from '@/types/currency'
import type { GeoPoint, PlaceSuggestion } from '@/types/place'
import { formatTwd, parseCost, toTwd } from '@/utils/money'
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
    initialDate?: string | null
    showCancel?: boolean
  }>(),
  {
    activity: null,
    rates: null,
    defaultCurrency: HOME_CURRENCY,
    near: null,
    prefill: null,
    initialDate: null,
    showCancel: false,
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

const name = ref(props.activity?.name ?? '')
const mode = ref<PlaceNameMode>(props.activity && !props.activity.place ? 'custom' : 'place')
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
const date = ref(
  props.activity?.date ?? prefilled?.date ?? props.initialDate ?? props.dates[0] ?? '',
)
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
    <PlaceNameField
      v-model:mode="mode"
      v-model:place="selectedPlace"
      v-model:name="name"
      label="Activity"
      :input-id="activityFieldId"
      input-name="name"
      :session-token="sessionToken"
      :near="near"
      placeholder="e.g. Free time"
      required
      :maxlength="MAX_ACTIVITY_NAME_LENGTH"
      custom-label="Not a place? Add a custom activity"
      custom-testid="use-custom-activity"
      place-testid="use-place-search"
    />

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

    <CostField
      v-model:amount="cost"
      v-model:currency="currency"
      label="Cost"
      amount-name="cost"
      currency-field-name="currency"
      currency-testid="currency-select"
      preview-testid="cost-preview"
      error-testid="cost-error"
      :rates="rates"
      :preview="costPreview"
      :error="costError"
    />

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
        :options="BOOKING_OPTIONS"
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
        v-if="isEditing || showCancel"
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

.field-error {
  margin-top: -0.6rem;
  font-size: 0.85rem;
  color: var(--color-danger);
}

.actions {
  display: flex;
  gap: 0.6rem;
  margin-top: 0.2rem;
}

.actions .btn {
  flex: 1;
}
@media (max-width: 720px) {
  .actions {
    position: sticky;
    bottom: 0;
    z-index: 2;
    margin: 0.2rem -0.25rem 0;
    padding: 0.75rem 0.25rem calc(0.25rem + env(safe-area-inset-bottom));
    background: linear-gradient(to bottom, rgb(255 255 255 / 0) 0, var(--color-surface) 0.75rem);
  }
}
</style>

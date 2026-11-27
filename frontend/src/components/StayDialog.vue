<script setup lang="ts">
import BaseDialog from '@/components/BaseDialog.vue'
import { computed, ref, useId, watch } from 'vue'
import { newSessionToken } from '@/utils/sessionToken'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import PlaceSearch from '@/components/PlaceSearch.vue'
import SelectMenu, { type SelectOption } from '@/components/SelectMenu.vue'
import type { BookingStatus } from '@/types/activity'
import { COMMON_CURRENCIES, HOME_CURRENCY, type ExchangeRates } from '@/types/currency'
import type { GeoPoint, PlaceSuggestion } from '@/types/place'
import { MAX_STAY_NAME_LENGTH, type SaveStayInput, type Stay } from '@/types/stay'
import { MAX_CONFIRMATION_CODE_LENGTH } from '@/types/activity'
import { BOOKING_STATUSES } from '@/utils/activityDetails'
import { countDays, formatDayLabel } from '@/utils/date'
import { currencyName, formatCurrency, formatTwd, parseCost, toTwd } from '@/utils/money'
import { checkOutDates, nextCheckInAfter, nightsLabel, quickCheckOuts } from '@/utils/stays'

const props = withDefaults(
  defineProps<{
    open: boolean
    dates: string[]
    submit: (input: SaveStayInput) => Promise<boolean>
    remove?: (stayId: string) => Promise<boolean>
    stay?: Stay | null
    stays?: Stay[]
    initialCheckIn?: string | null
    initialCheckOut?: string | null
    rates?: ExchangeRates | null
    defaultCurrency?: string
    near?: GeoPoint | null
    error?: string | null
  }>(),
  {
    remove: undefined,
    stay: null,
    stays: () => [],
    initialCheckIn: null,
    initialCheckOut: null,
    rates: null,
    defaultCurrency: HOME_CURRENCY,
    near: null,
    error: null,
  },
)

const emit = defineEmits<{
  close: []
  dismissError: []
}>()

const titleId = useId()
const nameFieldId = useId()

const mode = ref<'place' | 'custom'>('place')
const name = ref('')
const selectedPlace = ref<PlaceSuggestion | null>(null)
const sessionToken = ref(newSessionToken())
const checkIn = ref('')
const checkOut = ref('')
const cost = ref('')
const currency = ref(HOME_CURRENCY)
const bookingStatus = ref<BookingStatus>('NotNeeded')
const confirmationCode = ref('')
const submitting = ref(false)
const confirmingDelete = ref(false)

function reset() {
  const stay = props.stay
  mode.value = stay && !stay.place ? 'custom' : 'place'
  name.value = stay?.name ?? ''
  selectedPlace.value = stay?.place
    ? { placeId: stay.place.placeId, name: stay.name, description: stay.place.address }
    : null
  sessionToken.value = newSessionToken()
  checkIn.value = stay?.checkIn ?? props.initialCheckIn ?? props.dates[0] ?? ''
  checkOut.value = stay?.checkOut ?? props.initialCheckOut ?? defaultCheckOut(checkIn.value)
  cost.value = stay && stay.cost > 0 ? stay.cost.toFixed(2) : ''
  currency.value = stay?.currency ?? props.defaultCurrency
  bookingStatus.value = stay?.bookingStatus ?? 'NotNeeded'
  confirmationCode.value = stay?.confirmationCode ?? ''
  submitting.value = false
  confirmingDelete.value = false
}

function defaultCheckOut(from: string): string {
  return checkOutDates(props.dates).find((date) => date > from) ?? ''
}

watch(
  () => props.open,
  (open) => {
    if (open) {
      reset()
    }
  },
  { immediate: true },
)

watch(checkIn, (value) => {
  if (checkOut.value <= value) {
    checkOut.value = defaultCheckOut(value)
  }
})

const isEditing = computed(() => props.stay !== null)
const title = computed(() => (props.stay ? `Edit "${props.stay.name}"` : 'Add stay'))

const checkInOptions = computed<SelectOption[]>(() =>
  props.dates.map((date, index) => ({
    value: date,
    label: formatDayLabel(date),
    hint: `Day ${index + 1}`,
  })),
)

const checkOutOptions = computed<SelectOption[]>(() =>
  checkOutDates(props.dates)
    .filter((date) => date > checkIn.value)
    .map((date) => ({
      value: date,
      label: formatDayLabel(date),
      hint: nightsLabel(countDays(checkIn.value, date) - 1),
    })),
)

const lastFreeCheckOut = computed(() => {
  const others = props.stays.filter((other) => other.id !== props.stay?.id)
  return nextCheckInAfter(checkIn.value, others) ?? checkOutDates(props.dates).at(-1) ?? ''
})

const quickChoices = computed(() => quickCheckOuts(checkIn.value, lastFreeCheckOut.value))

const nights = computed(() =>
  checkIn.value && checkOut.value ? countDays(checkIn.value, checkOut.value) - 1 : 0,
)

const bookingOptions = BOOKING_STATUSES.map((status) => ({
  value: status.value,
  label: status.label,
}))

const currencyOptions = computed<SelectOption[]>(() => {
  const available = props.rates?.available ? new Set(props.rates.currencies) : null
  const codes: string[] = COMMON_CURRENCIES.filter((code) => !available || available.has(code))
  const withCurrent = codes.includes(currency.value) ? codes : [...codes, currency.value]

  return withCurrent.map((code) => ({ value: code, label: code, hint: currencyName(code) }))
})

const parsedCost = computed(() => parseCost(cost.value))
const costError = computed(() => (parsedCost.value.ok ? '' : parsedCost.value.error))
const costPreview = computed(() => {
  if (!parsedCost.value.ok || parsedCost.value.value === 0 || nights.value === 0) {
    return ''
  }

  const amount = parsedCost.value.value
  const perNight = `${formatCurrency(amount / nights.value, currency.value)} / night`

  if (currency.value === HOME_CURRENCY) {
    return perNight
  }

  const converted = toTwd(amount, currency.value, props.rates)
  return converted === null ? perNight : `${perNight} · ≈ ${formatTwd(converted)} total`
})

const hasName = computed(() =>
  mode.value === 'place' ? selectedPlace.value !== null : name.value.trim() !== '',
)

const canSubmit = computed(
  () =>
    hasName.value && checkIn.value !== '' && checkOut.value > checkIn.value && parsedCost.value.ok,
)

function useCustomName() {
  mode.value = 'custom'
  if (selectedPlace.value && name.value.trim() === '') {
    name.value = selectedPlace.value.name
  }
}

async function onSubmit() {
  if (!canSubmit.value || !parsedCost.value.ok) {
    return
  }

  submitting.value = true
  const place = mode.value === 'place' ? selectedPlace.value : null
  const saved = await props.submit({
    name: place ? place.name : name.value.trim(),
    ...(place ? { placeId: place.placeId, placeSessionToken: sessionToken.value } : {}),
    checkIn: checkIn.value,
    checkOut: checkOut.value,
    cost: parsedCost.value.value,
    currency: currency.value,
    bookingStatus: bookingStatus.value,
    confirmationCode: confirmationCode.value.trim(),
  })
  submitting.value = false

  if (saved) {
    emit('close')
  }
}

async function onConfirmDelete() {
  confirmingDelete.value = false

  if (props.stay && props.remove && (await props.remove(props.stay.id))) {
    emit('close')
  }
}
</script>

<template>
  <BaseDialog
    :open="open"
    variant="sheet"
    width="480px"
    :labelledby="titleId"
    testid="stay-dialog"
    backdrop-testid="stay-dialog-backdrop"
    @close="emit('close')"
  >
    <header class="dialog-header">
      <div>
        <span class="eyebrow">{{ isEditing ? 'Editing' : 'Where you sleep' }}</span>
        <h2 :id="titleId" class="section-title">{{ title }}</h2>
      </div>
      <button
        type="button"
        class="btn btn-ghost btn-small close"
        aria-label="Close"
        data-testid="close-stay-dialog"
        @click="emit('close')"
      >
        ✕
      </button>
    </header>

    <form class="dialog-body stay-form" data-testid="stay-form" @submit.prevent="onSubmit">
      <div class="field">
        <label class="field-label" :for="nameFieldId">Hotel or place</label>
        <PlaceSearch
          v-if="mode === 'place'"
          v-model="selectedPlace"
          :input-id="nameFieldId"
          :session-token="sessionToken"
          :near="near"
        />
        <input
          v-else
          :id="nameFieldId"
          v-model="name"
          class="text-input"
          name="stayName"
          type="text"
          placeholder="e.g. Airbnb in Shinjuku"
          autocomplete="off"
          :maxlength="MAX_STAY_NAME_LENGTH"
        />
        <button
          type="button"
          class="mode-switch"
          :data-testid="mode === 'place' ? 'use-custom-stay' : 'use-place-stay'"
          @click="mode === 'place' ? useCustomName() : (mode = 'place')"
        >
          {{ mode === 'place' ? 'Not on the map? Type a name' : 'Search a real place instead' }}
        </button>
      </div>

      <div class="date-row">
        <div class="field">
          <span class="field-label">Check-in</span>
          <SelectMenu
            v-model="checkIn"
            name="checkIn"
            :options="checkInOptions"
            data-testid="check-in-select"
          />
        </div>
        <div class="field">
          <span class="field-label">Check-out</span>
          <SelectMenu
            v-model="checkOut"
            name="checkOut"
            align="end"
            :options="checkOutOptions"
            data-testid="check-out-select"
          />
        </div>
      </div>
      <div class="nights-row">
        <p class="nights" data-testid="stay-nights">{{ nightsLabel(nights) }}</p>
        <div
          v-if="quickChoices.length > 1"
          class="quick-nights"
          role="group"
          aria-label="Quick check-out"
        >
          <button
            v-for="choice in quickChoices"
            :key="choice.checkOut"
            type="button"
            class="quick-chip"
            :aria-pressed="checkOut === choice.checkOut"
            data-testid="quick-check-out"
            @click="checkOut = choice.checkOut"
          >
            {{ choice.label }}
          </button>
        </div>
      </div>

      <div class="field">
        <span class="field-label">
          Total cost
          <span class="field-optional">Optional</span>
        </span>
        <div class="cost-row" :class="{ invalid: costError }">
          <input
            v-model="cost"
            class="text-input cost-amount"
            name="stayCost"
            type="text"
            inputmode="decimal"
            placeholder="0.00"
            autocomplete="off"
            aria-label="Total cost"
            :aria-invalid="costError !== ''"
          />
          <SelectMenu
            v-model="currency"
            class="cost-currency"
            align="end"
            name="stayCurrency"
            :options="currencyOptions"
            data-testid="stay-currency-select"
          />
        </div>
        <span v-if="costPreview" class="cost-preview" data-testid="stay-cost-preview">
          {{ costPreview }}
        </span>
        <p v-if="costError" class="field-error" data-testid="stay-cost-error">
          {{ costError }}
        </p>
      </div>

      <div class="date-row">
        <div class="field">
          <span class="field-label">Booking</span>
          <SelectMenu
            v-model="bookingStatus"
            name="stayBookingStatus"
            :options="bookingOptions"
            data-testid="stay-booking-select"
          />
        </div>
        <label class="field">
          <span class="field-label">Confirmation code</span>
          <input
            v-model="confirmationCode"
            class="text-input"
            name="stayConfirmationCode"
            type="text"
            autocomplete="off"
            :maxlength="MAX_CONFIRMATION_CODE_LENGTH"
          />
        </label>
      </div>

      <div v-if="error" role="alert" class="alert" data-testid="stay-error">
        <span>{{ error }}</span>
        <button
          type="button"
          class="alert-close"
          aria-label="Dismiss"
          data-testid="dismiss-stay-error"
          @click="emit('dismissError')"
        >
          ✕
        </button>
      </div>

      <div class="actions">
        <button
          v-if="isEditing && remove"
          type="button"
          class="btn btn-ghost danger"
          data-testid="delete-stay"
          @click="confirmingDelete = true"
        >
          Delete
        </button>
        <button type="button" class="btn btn-secondary" @click="emit('close')">Cancel</button>
        <button
          type="submit"
          class="btn btn-primary"
          data-testid="save-stay"
          :disabled="submitting || !canSubmit"
        >
          <template v-if="isEditing">{{ submitting ? 'Saving...' : 'Save' }}</template>
          <template v-else>{{ submitting ? 'Adding...' : 'Add stay' }}</template>
        </button>
      </div>
    </form>
  </BaseDialog>

  <ConfirmDialog
    :open="confirmingDelete"
    title="Delete stay?"
    :message="stay ? `Delete &quot;${stay.name}&quot; from your trip?` : ''"
    confirm-label="Delete"
    @confirm="onConfirmDelete"
    @cancel="confirmingDelete = false"
  />
</template>

<style scoped>
.dialog-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.4rem 1.5rem 0.75rem;
}

.dialog-header .section-title {
  margin-top: 0.15rem;
  overflow-wrap: anywhere;
}

.dialog-body {
  padding: 0.25rem 1.5rem 1.5rem;
  overflow-y: auto;
}

.stay-form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.date-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 0.6rem;
}

.nights-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 0.75rem;
  margin-top: -0.6rem;
}

.quick-nights {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-left: auto;
}

.quick-chip {
  padding: 0.2rem 0.65rem;
  font: inherit;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.quick-chip:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.quick-chip[aria-pressed='true'] {
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-color: var(--color-primary);
}

.nights {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-muted);
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

.field-optional {
  font-weight: 500;
  color: var(--color-subtle);
}

.field-error {
  font-size: 0.85rem;
  color: var(--color-danger);
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

.actions .btn-primary,
.actions .btn-secondary {
  flex: 1;
}

.danger {
  color: var(--color-danger);
}

@media (max-width: 560px) {
  .date-row {
    grid-template-columns: minmax(0, 1fr);
  }
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

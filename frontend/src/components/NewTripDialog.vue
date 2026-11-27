<script setup lang="ts">
import BaseDialog from '@/components/BaseDialog.vue'
import { computed, nextTick, ref, useId, watch } from 'vue'
import { newSessionToken } from '@/utils/sessionToken'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import PlaceSearch from '@/components/PlaceSearch.vue'
import type { PlaceSuggestion } from '@/types/place'
import { MAX_TRIP_DAYS, type CreateTripInput } from '@/types/trip'
import { countDays } from '@/utils/date'
import { daysLabel } from '@/utils/trips'

const props = withDefaults(
  defineProps<{
    open: boolean
    submit: (input: CreateTripInput) => Promise<boolean>
    error?: string | null
  }>(),
  {
    error: null,
  },
)

const emit = defineEmits<{
  close: []
}>()

const titleId = useId()
const destinationId = useId()
const dialog = ref<InstanceType<typeof BaseDialog> | null>(null)

const datePickerRange = { partialRange: false, maxRange: MAX_TRIP_DAYS - 1 }
const datePickerFormats = { input: 'MMM d, yyyy' }
const datePickerTimeConfig = { enableTimePicker: false }
const datePickerInputAttrs = { name: 'dates', clearable: true }

const destination = ref<PlaceSuggestion | null>(null)
const name = ref('')
const autoName = ref('')
const dates = ref<string[] | null>(null)
const submitting = ref(false)
const sessionToken = ref(newSessionToken())

const startDate = computed(() => dates.value?.[0] ?? '')
const endDate = computed(() => dates.value?.[1] ?? '')
const datesHint = computed(() =>
  startDate.value && endDate.value
    ? daysLabel(countDays(startDate.value, endDate.value))
    : `Up to ${MAX_TRIP_DAYS} days`,
)
const nameHint = computed(() =>
  destination.value && name.value === autoName.value ? 'From the destination' : '',
)
const canSubmit = computed(
  () => name.value.trim() !== '' && startDate.value !== '' && endDate.value !== '',
)

watch(destination, (place) => {
  if (!place) {
    return
  }

  if (name.value.trim() === '' || name.value === autoName.value) {
    name.value = place.name
  }

  autoName.value = place.name
})

watch(
  () => props.open,
  async (open) => {
    if (!open) {
      return
    }

    destination.value = null
    name.value = ''
    autoName.value = ''
    dates.value = null
    sessionToken.value = newSessionToken()
    await nextTick()
    dialog.value?.panel
      ?.querySelector<HTMLInputElement>('[data-testid="place-search-input"]')
      ?.focus()
  },
  { immediate: true },
)

async function onSubmit() {
  if (!canSubmit.value || submitting.value) {
    return
  }

  submitting.value = true

  const created = await props.submit({
    name: name.value,
    startDate: startDate.value,
    endDate: endDate.value,
    ...(destination.value
      ? { destinationPlaceId: destination.value.placeId, placeSessionToken: sessionToken.value }
      : {}),
  })

  submitting.value = false

  if (created) {
    emit('close')
  }
}
</script>

<template>
  <BaseDialog
    ref="dialog"
    :open="open"
    tag="form"
    :labelledby="titleId"
    testid="new-trip-dialog"
    backdrop-testid="new-trip-backdrop"
    @close="emit('close')"
    @submit="onSubmit"
  >
    <span class="eyebrow">Start planning</span>
    <h2 :id="titleId" class="title">New trip</h2>

    <div class="field">
      <label class="field-label" :for="destinationId">
        Where to?
        <span class="field-hint">Optional</span>
      </label>
      <PlaceSearch
        v-model="destination"
        :session-token="sessionToken"
        :input-id="destinationId"
        kind="destination"
        placeholder="Search a city, e.g. Tokyo"
      />
    </div>

    <label class="field">
      <span class="field-label">
        Name
        <span class="field-hint" data-testid="name-hint">{{ nameHint }}</span>
      </span>
      <input
        v-model="name"
        class="text-input"
        name="name"
        type="text"
        placeholder="e.g. Tokyo with friends"
        autocomplete="off"
        required
        maxlength="100"
      />
    </label>

    <div class="field">
      <span class="field-label">
        Dates
        <span class="field-hint" data-testid="dates-hint">{{ datesHint }}</span>
      </span>
      <VueDatePicker
        v-model="dates"
        :range="datePickerRange"
        auto-apply
        model-type="yyyy-MM-dd"
        placeholder="Select dates"
        :formats="datePickerFormats"
        :time-config="datePickerTimeConfig"
        :input-attrs="datePickerInputAttrs"
        teleport
      />
    </div>

    <div v-if="error" role="alert" class="alert" data-testid="new-trip-error">
      {{ error }}
    </div>

    <div class="actions">
      <button
        type="button"
        class="btn btn-secondary"
        data-testid="cancel-new-trip"
        @click="emit('close')"
      >
        Cancel
      </button>
      <button
        type="submit"
        class="btn btn-primary"
        data-testid="create-trip"
        :disabled="submitting || !canSubmit"
      >
        {{ submitting ? 'Creating...' : 'Create trip' }}
      </button>
    </div>
  </BaseDialog>
</template>

<style scoped>
.title {
  margin-top: -0.85rem;
  font-family: var(--font-display);
  font-size: 1.45rem;
  font-weight: 700;
}

.actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
  margin-top: 0.25rem;
}
</style>

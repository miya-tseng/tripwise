<script setup lang="ts">
import { computed, ref } from 'vue'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import { MAX_TRIP_DAYS, type CreateTripInput } from '@/types/trip'
import { countDays } from '@/utils/date'

const props = defineProps<{
  submit: (input: CreateTripInput) => Promise<boolean>
}>()

const datePickerRange = { partialRange: false, maxRange: MAX_TRIP_DAYS - 1 }
const datePickerFormats = { input: 'MMM d, yyyy' }
const datePickerTimeConfig = { enableTimePicker: false }
const datePickerInputAttrs = { name: 'dates', clearable: true }

const name = ref('')
const dates = ref<string[] | null>(null)
const submitting = ref(false)

const startDate = computed(() => dates.value?.[0] ?? '')
const endDate = computed(() => dates.value?.[1] ?? '')
const datesHint = computed(() => {
  if (!startDate.value || !endDate.value) {
    return `Up to ${MAX_TRIP_DAYS} days`
  }

  const days = countDays(startDate.value, endDate.value)
  return `${days} ${days === 1 ? 'day' : 'days'}`
})
const canSubmit = computed(
  () => name.value.trim() !== '' && startDate.value !== '' && endDate.value !== '',
)

async function onSubmit() {
  if (!canSubmit.value) {
    return
  }

  submitting.value = true

  const created = await props.submit({
    name: name.value,
    startDate: startDate.value,
    endDate: endDate.value,
  })

  submitting.value = false

  if (created) {
    name.value = ''
    dates.value = null
  }
}
</script>

<template>
  <form class="trip-form" @submit.prevent="onSubmit">
    <label class="field">
      <span class="field-label">Name</span>
      <input
        v-model="name"
        class="text-input"
        name="name"
        type="text"
        placeholder="e.g. Tokyo"
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
      />
    </div>
    <button type="submit" class="btn btn-primary" :disabled="submitting || !canSubmit">
      {{ submitting ? 'Creating...' : 'Create' }}
    </button>
  </form>
</template>

<style scoped>
.trip-form {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.3fr) auto;
  gap: 1rem;
  align-items: end;
}

.trip-form .btn {
  min-width: 7rem;
}

@media (max-width: 720px) {
  .trip-form {
    grid-template-columns: 1fr;
  }
}
</style>

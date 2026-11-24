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
  grid-template-columns: 1fr 1.4fr auto;
  gap: 1rem;
  align-items: end;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
}

.field-label {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-muted);
}

.field-hint {
  font-weight: 500;
  color: var(--color-primary);
}

.text-input {
  width: 100%;
  height: 2.75rem;
  padding: 0 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: #fff;
  color: var(--color-text);
  transition:
    border-color 0.15s,
    box-shadow 0.15s;
}

.text-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15);
}

.trip-form .btn {
  height: 2.75rem;
}

@media (max-width: 640px) {
  .trip-form {
    grid-template-columns: 1fr;
  }
}
</style>

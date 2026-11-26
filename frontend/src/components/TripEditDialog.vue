<script setup lang="ts">
import { computed, nextTick, ref, useId, watch } from 'vue'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import { MAX_TRIP_DAYS, type Trip, type UpdateTripInput } from '@/types/trip'
import { countDays } from '@/utils/date'

const props = withDefaults(
  defineProps<{
    open: boolean
    trip: Trip
    submit: (input: UpdateTripInput) => Promise<boolean>
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
const nameInput = ref<HTMLInputElement | null>(null)

const datePickerRange = { partialRange: false, maxRange: MAX_TRIP_DAYS - 1 }
const datePickerFormats = { input: 'MMM d, yyyy' }
const datePickerTimeConfig = { enableTimePicker: false }
const datePickerInputAttrs = { name: 'edit-dates', clearable: false }

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

watch(
  () => props.open,
  async (open) => {
    if (open) {
      name.value = props.trip.name
      dates.value = [props.trip.startDate, props.trip.endDate]
      await nextTick()
      nameInput.value?.focus()
    }
  },
  { immediate: true },
)

async function onSubmit() {
  if (!canSubmit.value || submitting.value) {
    return
  }

  submitting.value = true

  const saved = await props.submit({
    name: name.value,
    startDate: startDate.value,
    endDate: endDate.value,
  })

  submitting.value = false

  if (saved) {
    emit('close')
  }
}
</script>

<template>
  <Transition name="dialog">
    <div v-if="open" class="backdrop" data-testid="edit-trip-backdrop" @click.self="emit('close')">
      <form
        class="dialog"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        data-testid="edit-trip-dialog"
        @submit.prevent="onSubmit"
        @keydown.esc="emit('close')"
      >
        <span class="eyebrow">Trip details</span>
        <h2 :id="titleId" class="title">Edit trip</h2>

        <label class="field">
          <span class="field-label">Name</span>
          <input
            ref="nameInput"
            v-model="name"
            class="text-input"
            name="edit-name"
            type="text"
            autocomplete="off"
            required
            maxlength="100"
          />
        </label>

        <div class="field">
          <span class="field-label">
            Dates
            <span class="field-hint" data-testid="edit-dates-hint">{{ datesHint }}</span>
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

        <div v-if="error" role="alert" class="alert" data-testid="edit-trip-error">
          {{ error }}
        </div>

        <div class="actions">
          <button
            type="button"
            class="btn btn-secondary"
            data-testid="cancel-edit-trip"
            @click="emit('close')"
          >
            Cancel
          </button>
          <button type="submit" class="btn btn-primary" :disabled="submitting || !canSubmit">
            {{ submitting ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  </Transition>
</template>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(22 30 40 / 0.42);
  backdrop-filter: blur(4px);
}

.dialog {
  width: 100%;
  max-width: 440px;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  padding: 1.75rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

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

.dialog-enter-active,
.dialog-leave-active {
  transition: opacity 0.2s ease;
}

.dialog-enter-active .dialog,
.dialog-leave-active .dialog {
  transition: transform 0.2s ease;
}

.dialog-enter-from,
.dialog-leave-to {
  opacity: 0;
}

.dialog-enter-from .dialog,
.dialog-leave-to .dialog {
  transform: scale(0.96) translateY(8px);
}
</style>

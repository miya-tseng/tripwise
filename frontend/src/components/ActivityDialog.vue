<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import ActivityForm, { type ActivityPrefill } from '@/components/ActivityForm.vue'
import type { Activity, SaveActivityInput } from '@/types/activity'
import type { ExchangeRates } from '@/types/currency'
import type { GeoPoint } from '@/types/place'
import { formatDayLabel } from '@/utils/date'

const props = withDefaults(
  defineProps<{
    open: boolean
    dates: string[]
    submit: (input: SaveActivityInput) => Promise<boolean>
    activity?: Activity | null
    prefill?: ActivityPrefill | null
    initialDate?: string | null
    rates?: ExchangeRates | null
    defaultCurrency?: string
    near?: GeoPoint | null
    error?: string | null
    formKey?: string
    remove?: (activityId: string) => Promise<boolean>
  }>(),
  {
    activity: null,
    prefill: null,
    initialDate: null,
    rates: null,
    defaultCurrency: 'TWD',
    near: null,
    error: null,
    formKey: 'activity',
    remove: undefined,
  },
)

const emit = defineEmits<{
  close: []
  dismissError: []
}>()

const titleId = useId()
const confirmingDelete = ref(false)

watch(
  () => props.open,
  () => {
    confirmingDelete.value = false
  },
)

async function onConfirmDelete() {
  const activity = props.activity
  confirmingDelete.value = false

  if (activity && props.remove && (await props.remove(activity.id))) {
    emit('close')
  }
}

const title = computed(() => (props.activity ? `Edit "${props.activity.name}"` : 'Add activity'))
const subtitle = computed(() => {
  const date = props.activity?.date ?? props.prefill?.date ?? props.initialDate ?? props.dates[0]
  if (!date) {
    return ''
  }
  const dayNumber = props.dates.indexOf(date) + 1
  return dayNumber > 0 ? `Day ${dayNumber} · ${formatDayLabel(date)}` : formatDayLabel(date)
})

async function save(input: SaveActivityInput): Promise<boolean> {
  const saved = await props.submit(input)

  if (saved) {
    emit('close')
  }

  return saved
}
</script>

<template>
  <Transition name="dialog">
    <div
      v-if="open"
      class="backdrop"
      data-testid="activity-dialog-backdrop"
      @click.self="emit('close')"
    >
      <section
        class="dialog"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        data-testid="activity-dialog"
        @keydown.esc="emit('close')"
      >
        <header class="dialog-header">
          <div>
            <span class="eyebrow">{{ activity ? 'Editing' : 'Plan' }}</span>
            <h2 :id="titleId" class="section-title">{{ title }}</h2>
            <p v-if="subtitle && !activity" class="subtitle">{{ subtitle }}</p>
          </div>
          <button
            type="button"
            class="btn btn-ghost btn-small close"
            aria-label="Close"
            data-testid="close-activity-dialog"
            @click="emit('close')"
          >
            ✕
          </button>
        </header>

        <div class="dialog-body">
          <ActivityForm
            :key="formKey"
            :dates="dates"
            :activity="activity"
            :prefill="prefill"
            :initial-date="initialDate"
            :submit="save"
            :rates="rates"
            :default-currency="defaultCurrency"
            :near="near"
            show-cancel
            @cancel="emit('close')"
          />
          <button
            v-if="activity && remove"
            type="button"
            class="btn btn-ghost btn-small delete-activity"
            data-testid="dialog-delete-activity"
            @click="confirmingDelete = true"
          >
            Delete this activity
          </button>
          <div v-if="error" role="alert" class="alert form-alert" data-testid="schedule-error">
            <span>{{ error }}</span>
            <button
              type="button"
              class="alert-close"
              aria-label="Dismiss"
              data-testid="dismiss-error"
              @click="emit('dismissError')"
            >
              ✕
            </button>
          </div>
        </div>
      </section>
    </div>
  </Transition>

  <ConfirmDialog
    :open="confirmingDelete"
    title="Delete activity?"
    :message="activity ? `Delete &quot;${activity.name}&quot; from your trip?` : ''"
    confirm-label="Delete"
    @confirm="onConfirmDelete"
    @cancel="confirmingDelete = false"
  />
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
  max-width: 460px;
  max-height: calc(100vh - 2rem);
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

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

.subtitle {
  margin-top: 0.1rem;
  font-size: 0.85rem;
  color: var(--color-muted);
}

.dialog-body {
  padding: 0.25rem 1.5rem 1.5rem;
  overflow-y: auto;
}

.form-alert {
  margin-top: 1rem;
}

.dialog-enter-active,
.dialog-leave-active {
  transition: opacity 0.2s ease;
}

.dialog-enter-from,
.dialog-leave-to {
  opacity: 0;
}

@media (max-width: 560px) {
  .backdrop {
    place-items: end stretch;
    padding: 0;
  }

  .dialog {
    max-width: none;
    max-height: 92vh;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  }
}
.delete-activity {
  align-self: flex-start;
  margin-top: 0.75rem;
  color: var(--color-danger);
}

.delete-activity:hover {
  background: var(--color-danger-soft);
}
</style>

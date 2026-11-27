<script setup lang="ts">
import { computed, nextTick, ref, useId } from 'vue'
import { useClickOutside } from '@/composables/useClickOutside'
import { useFloatingPanel } from '@/composables/useFloatingPanel'

const props = withDefaults(
  defineProps<{
    modelValue: string
    name?: string
    label?: string
    placeholder?: string
    minuteStep?: number
    align?: 'start' | 'end'
  }>(),
  {
    name: undefined,
    label: undefined,
    placeholder: '--:--',
    minuteStep: 5,
    align: 'start',
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const panelId = useId()
const root = ref<HTMLElement | null>(null)
const trigger = ref<HTMLButtonElement | null>(null)
const panel = ref<HTMLElement | null>(null)
const open = ref(false)
const floating = useFloatingPanel(trigger, panel, open, { align: props.align, matchWidth: false })

const pad = (value: number) => String(value).padStart(2, '0')

const selectedHour = computed(() => props.modelValue.slice(0, 2))
const selectedMinute = computed(() => props.modelValue.slice(3, 5))

const hours = Array.from({ length: 24 }, (_, hour) => pad(hour))
const minutes = computed(() => {
  const steps = Array.from({ length: Math.ceil(60 / props.minuteStep) }, (_, index) =>
    pad(index * props.minuteStep),
  )
  const current = selectedMinute.value
  return current && !steps.includes(current) ? [...steps, current].sort() : steps
})

useClickOutside(root, open, () => close())

async function scrollSelectedIntoView() {
  await nextTick()
  panel.value?.querySelectorAll<HTMLElement>('.time-option.selected').forEach((option) => {
    const column = option.parentElement
    if (column) {
      column.scrollTop = option.offsetTop - column.clientHeight / 2 + option.clientHeight / 2
    }
  })
}

function openPanel() {
  open.value = true
  scrollSelectedIntoView()
}

function close(returnFocus = false) {
  open.value = false
  if (returnFocus) {
    trigger.value?.focus()
  }
}

function toggle() {
  if (open.value) {
    close()
  } else {
    openPanel()
  }
}

function chooseHour(hour: string) {
  emit('update:modelValue', `${hour}:${selectedMinute.value || '00'}`)
}

function chooseMinute(minute: string) {
  emit('update:modelValue', `${selectedHour.value || '09'}:${minute}`)
  close(true)
}

function onTriggerKeydown(event: KeyboardEvent) {
  if (open.value && event.key === 'Escape') {
    event.preventDefault()
    close()
    return
  }

  if (!open.value && ['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
    event.preventDefault()
    openPanel()
  }
}

function onPanelKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    close(true)
  }
}
</script>

<template>
  <div ref="root" class="time-picker" :class="{ open }">
    <button
      ref="trigger"
      type="button"
      class="text-input time-trigger"
      aria-haspopup="dialog"
      :aria-expanded="open"
      :aria-controls="panelId"
      :aria-label="label ? `${label}: ${modelValue || 'not set'}` : undefined"
      data-testid="time-trigger"
      @click="toggle"
      @keydown="onTriggerKeydown"
    >
      <svg class="clock" viewBox="0 0 20 20" aria-hidden="true">
        <circle cx="10" cy="10" r="7" fill="none" stroke="currentColor" stroke-width="1.6" />
        <path
          d="M10 6.5V10l2.3 1.6"
          fill="none"
          stroke="currentColor"
          stroke-width="1.6"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
      <span v-if="modelValue" class="time-value" data-testid="time-value">{{ modelValue }}</span>
      <span v-else class="time-placeholder">{{ placeholder }}</span>
    </button>
    <input v-if="name" type="hidden" :name="name" :value="modelValue" />

    <Transition name="menu">
      <div
        v-show="open"
        :id="panelId"
        ref="panel"
        class="popover-menu time-panel"
        :class="`placement-${floating.placement.value}`"
        :style="floating.style.value"
        role="dialog"
        :aria-label="label ? `Choose ${label.toLowerCase()} time` : 'Choose time'"
        data-testid="time-panel"
        @keydown="onPanelKeydown"
      >
        <div class="column-labels" aria-hidden="true">
          <span>Hour</span>
          <span>Min</span>
        </div>
        <div class="columns">
          <div class="column" role="group" aria-label="Hour" data-testid="hour-column">
            <button
              v-for="hour in hours"
              :key="hour"
              type="button"
              class="time-option"
              :class="{ selected: hour === selectedHour }"
              :aria-pressed="hour === selectedHour"
              data-testid="hour-option"
              @click="chooseHour(hour)"
            >
              {{ hour }}
            </button>
          </div>
          <div class="column" role="group" aria-label="Minute" data-testid="minute-column">
            <button
              v-for="minute in minutes"
              :key="minute"
              type="button"
              class="time-option"
              :class="{ selected: minute === selectedMinute }"
              :aria-pressed="minute === selectedMinute"
              data-testid="minute-option"
              @click="chooseMinute(minute)"
            >
              {{ minute }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.time-picker {
  position: relative;
}

.time-trigger {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  text-align: left;
  cursor: pointer;
}

.clock {
  flex-shrink: 0;
  width: 1.1rem;
  height: 1.1rem;
  color: var(--color-subtle);
  transition: color 0.15s;
}

.open .clock {
  color: var(--color-primary);
}

.time-value {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.02em;
}

.time-placeholder {
  color: var(--color-subtle);
  letter-spacing: 0.08em;
}

.time-panel {
  position: fixed;
  z-index: 300;
  display: flex;
  flex-direction: column;
  width: 10.5rem;
  padding: 0.5rem;
}

.column-labels,
.columns {
  min-height: 0;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.35rem;
}

.column-labels {
  padding: 0.15rem 0 0.4rem;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-align: center;
  text-transform: uppercase;
  color: var(--color-subtle);
  border-bottom: 1px solid var(--color-border);
}

.column {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  max-height: 13.5rem;
  padding: 0.35rem 0.1rem;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--color-border-strong) transparent;
}

.time-option {
  flex-shrink: 0;
  height: 2.15rem;
  border: none;
  border-radius: 8px;
  background: transparent;
  font-weight: 500;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
  cursor: pointer;
  transition:
    background-color 0.12s,
    color 0.12s;
}

.time-option:hover {
  background: var(--color-primary-soft);
}

.time-option:focus-visible {
  outline: none;
  box-shadow: inset 0 0 0 2px var(--color-primary);
}

.time-option.selected {
  font-weight: 700;
  color: #fff;
  background: var(--color-primary);
}

.menu-enter-active,
.menu-leave-active {
  transition:
    opacity 0.14s ease,
    transform 0.14s ease;
}

.menu-enter-from,
.menu-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}
</style>

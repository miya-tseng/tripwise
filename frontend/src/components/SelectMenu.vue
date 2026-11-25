<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, useId, watch } from 'vue'

export interface SelectOption {
  value: string
  label: string
  hint?: string
}

const props = defineProps<{
  modelValue: string
  options: SelectOption[]
  name?: string
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const listboxId = useId()
const root = ref<HTMLElement | null>(null)
const trigger = ref<HTMLButtonElement | null>(null)
const listbox = ref<HTMLUListElement | null>(null)
const open = ref(false)
const activeIndex = ref(-1)

const selectedIndex = computed(() =>
  props.options.findIndex((option) => option.value === props.modelValue),
)
const selected = computed(() => props.options[selectedIndex.value] ?? null)
const activeOptionId = computed(() =>
  open.value && activeIndex.value >= 0 ? `${listboxId}-${activeIndex.value}` : undefined,
)

function onDocumentPointerDown(event: PointerEvent) {
  if (root.value && !root.value.contains(event.target as Node)) {
    close()
  }
}

watch(open, (isOpen) => {
  if (isOpen) {
    document.addEventListener('pointerdown', onDocumentPointerDown)
  } else {
    document.removeEventListener('pointerdown', onDocumentPointerDown)
  }
})

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocumentPointerDown)
})

async function scrollActiveIntoView() {
  await nextTick()
  listbox.value
    ?.querySelector<HTMLElement>(`[data-index="${activeIndex.value}"]`)
    ?.scrollIntoView?.({ block: 'nearest' })
}

function openMenu() {
  open.value = true
  activeIndex.value = Math.max(selectedIndex.value, 0)
  scrollActiveIntoView()
}

function close() {
  open.value = false
  activeIndex.value = -1
}

function toggle() {
  if (open.value) {
    close()
  } else {
    openMenu()
  }
}

function choose(index: number) {
  const option = props.options[index]
  if (option) {
    emit('update:modelValue', option.value)
  }
  close()
  trigger.value?.focus()
}

function moveActive(step: number) {
  const count = props.options.length
  if (count === 0) {
    return
  }
  activeIndex.value = (activeIndex.value + step + count) % count
  scrollActiveIntoView()
}

function onKeydown(event: KeyboardEvent) {
  if (!open.value) {
    if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
      event.preventDefault()
      openMenu()
    }
    return
  }

  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      moveActive(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      moveActive(-1)
      break
    case 'Home':
      event.preventDefault()
      activeIndex.value = 0
      scrollActiveIntoView()
      break
    case 'End':
      event.preventDefault()
      activeIndex.value = props.options.length - 1
      scrollActiveIntoView()
      break
    case 'Enter':
    case ' ':
      event.preventDefault()
      choose(activeIndex.value)
      break
    case 'Escape':
      event.preventDefault()
      close()
      break
    case 'Tab':
      close()
      break
  }
}
</script>

<template>
  <div ref="root" class="select" :class="{ open }">
    <button
      ref="trigger"
      type="button"
      class="text-input select-trigger"
      role="combobox"
      aria-haspopup="listbox"
      :aria-expanded="open"
      :aria-controls="listboxId"
      :aria-activedescendant="activeOptionId"
      data-testid="select-trigger"
      @click="toggle"
      @keydown="onKeydown"
    >
      <span v-if="selected" class="select-value">
        <span class="select-label">{{ selected.label }}</span>
        <span v-if="selected.hint" class="select-hint">{{ selected.hint }}</span>
      </span>
      <span v-else class="select-placeholder">{{ placeholder ?? 'Select' }}</span>
      <svg class="chevron" viewBox="0 0 20 20" aria-hidden="true">
        <path
          d="M5.5 7.5 10 12l4.5-4.5"
          fill="none"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </button>
    <input v-if="name" type="hidden" :name="name" :value="modelValue" />

    <Transition name="menu">
      <ul
        v-show="open"
        :id="listboxId"
        ref="listbox"
        class="popover-menu select-menu"
        role="listbox"
        tabindex="-1"
        data-testid="select-menu"
      >
        <li
          v-for="(option, index) in options"
          :id="`${listboxId}-${index}`"
          :key="option.value"
          class="select-option"
          :class="{ active: index === activeIndex, selected: option.value === modelValue }"
          role="option"
          :aria-selected="option.value === modelValue"
          :data-index="index"
          data-testid="select-option"
          @pointerenter="activeIndex = index"
          @mousedown.prevent
          @click="choose(index)"
        >
          <span class="option-text">
            <span class="option-label">{{ option.label }}</span>
            <span v-if="option.hint" class="option-hint">{{ option.hint }}</span>
          </span>
          <svg
            v-if="option.value === modelValue"
            class="check"
            viewBox="0 0 20 20"
            aria-hidden="true"
          >
            <path
              d="m5 10.5 3.2 3.2L15 7"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </li>
      </ul>
    </Transition>
  </div>
</template>

<style scoped>
.select {
  position: relative;
}

.select-trigger {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-align: left;
  cursor: pointer;
}

.select-value {
  flex: 1;
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
}

.select-label {
  font-weight: 600;
}

.select-hint {
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--color-muted);
}

.select-placeholder {
  flex: 1;
  color: var(--color-subtle);
}

.chevron {
  flex-shrink: 0;
  width: 1.15rem;
  height: 1.15rem;
  color: var(--color-subtle);
  transition: transform 0.2s ease;
}

.open .chevron {
  transform: rotate(180deg);
  color: var(--color-primary);
}

.select-menu {
  position: absolute;
  z-index: 50;
  top: calc(100% + 0.4rem);
  left: 0;
  right: 0;
  max-height: 16rem;
  margin: 0;
  padding: 0.35rem;
  overflow-y: auto;
  list-style: none;
}

.select-menu:focus {
  outline: none;
}

.select-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.55rem 0.7rem;
  border-radius: 10px;
  cursor: pointer;
  transition: background-color 0.1s;
}

.select-option.active {
  background: var(--color-primary-soft);
}

.option-text {
  flex: 1;
  display: flex;
  align-items: baseline;
  gap: 0.6rem;
  min-width: 0;
}

.option-label {
  font-weight: 600;
  min-width: 3.25rem;
}

.option-hint {
  color: var(--color-muted);
}

.selected .option-label,
.selected .option-hint {
  color: var(--color-primary);
}

.check {
  flex-shrink: 0;
  width: 1.1rem;
  height: 1.1rem;
  color: var(--color-primary);
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

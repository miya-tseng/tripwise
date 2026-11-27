<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, useId } from 'vue'
import { errorMessage } from '@/utils/errors'
import { useClickOutside } from '@/composables/useClickOutside'
import { searchPlaces } from '@/api/places'
import { useFloatingPanel } from '@/composables/useFloatingPanel'
import {
  MIN_PLACE_QUERY_LENGTH,
  type GeoPoint,
  type PlaceSearchKind,
  type PlaceSuggestion,
} from '@/types/place'

const props = withDefaults(
  defineProps<{
    modelValue: PlaceSuggestion | null
    sessionToken: string
    near?: GeoPoint | null
    inputId?: string
    debounceMs?: number
    kind?: PlaceSearchKind
    placeholder?: string
  }>(),
  {
    near: null,
    kind: 'any',
    placeholder: 'Search a place, e.g. Senso-ji',
    inputId: undefined,
    debounceMs: 250,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: PlaceSuggestion | null]
}>()

const listboxId = useId()
const root = ref<HTMLElement | null>(null)
const input = ref<HTMLInputElement | null>(null)
const query = ref('')
const suggestions = ref<PlaceSuggestion[]>([])
const open = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)
const activeIndex = ref(-1)
const searchBox = ref<HTMLElement | null>(null)
const panelElement = ref<HTMLElement | null>(null)

let debounceTimer: ReturnType<typeof setTimeout> | undefined
let latestRequest = 0

const trimmedQuery = computed(() => query.value.trim())
const showPanel = computed(() => open.value && trimmedQuery.value.length >= MIN_PLACE_QUERY_LENGTH)
const floating = useFloatingPanel(searchBox, panelElement, showPanel)
const activeOptionId = computed(() =>
  showPanel.value && activeIndex.value >= 0 ? `${listboxId}-${activeIndex.value}` : undefined,
)

useClickOutside(root, open, () => (open.value = false))

onBeforeUnmount(() => {
  clearTimeout(debounceTimer)
})

async function runSearch(text: string) {
  const requestId = ++latestRequest
  loading.value = true
  error.value = null

  try {
    const results = await searchPlaces(text, props.sessionToken, props.near, props.kind)

    if (requestId === latestRequest) {
      suggestions.value = results
      activeIndex.value = results.length > 0 ? 0 : -1
    }
  } catch (e) {
    if (requestId === latestRequest) {
      suggestions.value = []
      activeIndex.value = -1
      error.value = errorMessage(e, 'Place search failed.')
    }
  } finally {
    if (requestId === latestRequest) {
      loading.value = false
    }
  }
}

function onInput() {
  open.value = true
  clearTimeout(debounceTimer)

  if (trimmedQuery.value.length < MIN_PLACE_QUERY_LENGTH) {
    latestRequest++
    suggestions.value = []
    loading.value = false
    error.value = null
    return
  }

  const text = trimmedQuery.value
  loading.value = true
  debounceTimer = setTimeout(() => runSearch(text), props.debounceMs)
}

function choose(index: number) {
  const suggestion = suggestions.value[index]

  if (!suggestion) {
    return
  }

  emit('update:modelValue', suggestion)
  open.value = false
  query.value = ''
  suggestions.value = []
}

async function change() {
  emit('update:modelValue', null)
  await nextTick()
  input.value?.focus()
}

function move(step: number) {
  const count = suggestions.value.length

  if (count === 0) {
    return
  }

  activeIndex.value = (activeIndex.value + step + count) % count
}

function onKeydown(event: KeyboardEvent) {
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      open.value = true
      move(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      move(-1)
      break
    case 'Enter':
      if (showPanel.value && activeIndex.value >= 0) {
        event.preventDefault()
        choose(activeIndex.value)
      }
      break
    case 'Escape':
      if (open.value) {
        event.preventDefault()
        event.stopPropagation()
        open.value = false
      }
      break
  }
}
</script>

<template>
  <div ref="root" class="place-search">
    <div v-if="modelValue" class="selected-place" data-testid="selected-place">
      <span class="pin" aria-hidden="true">
        <svg viewBox="0 0 20 20">
          <path
            d="M10 18s-5.5-5.2-5.5-9.3a5.5 5.5 0 1 1 11 0C15.5 12.8 10 18 10 18Z"
            fill="currentColor"
          />
          <circle cx="10" cy="8.6" r="2" fill="#fff" />
        </svg>
      </span>
      <span class="selected-text">
        <span class="selected-name" data-testid="selected-place-name">{{ modelValue.name }}</span>
        <span v-if="modelValue.description" class="selected-description">
          {{ modelValue.description }}
        </span>
      </span>
      <button
        type="button"
        class="btn btn-ghost btn-small"
        data-testid="change-place"
        @click="change"
      >
        Change
      </button>
    </div>

    <template v-else>
      <div ref="searchBox" class="search-input">
        <svg class="search-icon" viewBox="0 0 20 20" aria-hidden="true">
          <circle cx="9" cy="9" r="5.5" fill="none" stroke="currentColor" stroke-width="1.7" />
          <path
            d="m13.2 13.2 3.3 3.3"
            stroke="currentColor"
            stroke-width="1.7"
            stroke-linecap="round"
          />
        </svg>
        <input
          :id="inputId"
          ref="input"
          v-model="query"
          class="text-input"
          type="text"
          name="place-search"
          role="combobox"
          aria-autocomplete="list"
          :aria-expanded="showPanel"
          :aria-controls="listboxId"
          :aria-activedescendant="activeOptionId"
          :placeholder="placeholder"
          autocomplete="off"
          maxlength="100"
          data-testid="place-search-input"
          @input="onInput"
          @focus="open = true"
          @keydown="onKeydown"
        />
        <span v-if="loading && showPanel" class="spinner" aria-hidden="true" />
      </div>

      <div
        v-show="showPanel"
        ref="panelElement"
        class="popover-menu place-panel"
        :style="floating.style.value"
        data-testid="place-panel"
      >
        <p v-if="error" class="panel-message error" role="alert" data-testid="place-search-error">
          {{ error }}
        </p>
        <p
          v-else-if="!loading && suggestions.length === 0"
          class="panel-message"
          data-testid="place-search-empty"
        >
          No places found.
        </p>
        <ul
          v-show="suggestions.length > 0"
          :id="listboxId"
          class="place-options"
          role="listbox"
          aria-label="Places"
        >
          <li
            v-for="(suggestion, index) in suggestions"
            :id="`${listboxId}-${index}`"
            :key="suggestion.placeId"
            class="place-option"
            :class="{ active: index === activeIndex }"
            role="option"
            :aria-selected="index === activeIndex"
            data-testid="place-option"
            @pointerenter="activeIndex = index"
            @mousedown.prevent
            @click="choose(index)"
          >
            <span class="option-name">{{ suggestion.name }}</span>
            <span class="option-description">{{ suggestion.description }}</span>
          </li>
        </ul>
        <p class="powered-by">Powered by Google</p>
      </div>
    </template>
  </div>
</template>

<style scoped>
.place-search {
  position: relative;
}

.search-input {
  position: relative;
}

.search-input .text-input {
  padding-left: 2.5rem;
  padding-right: 2.5rem;
}

.search-icon {
  position: absolute;
  top: 50%;
  left: 0.85rem;
  width: 1.1rem;
  height: 1.1rem;
  margin-top: -0.55rem;
  color: var(--color-subtle);
  pointer-events: none;
}

.spinner {
  position: absolute;
  top: 50%;
  right: 0.9rem;
  width: 1rem;
  height: 1rem;
  margin-top: -0.5rem;
  border: 2px solid var(--color-primary-soft);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.place-panel {
  position: fixed;
  z-index: 300;
  display: flex;
  flex-direction: column;
  padding: 0.35rem;
}

.place-options {
  min-height: 0;
  max-height: 18rem;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  list-style: none;
}

.place-option {
  display: flex;
  flex-direction: column;
  gap: 0.05rem;
  padding: 0.55rem 0.75rem;
  border-radius: 10px;
  cursor: pointer;
}

.place-option.active {
  background: var(--color-primary-soft);
}

.option-name {
  font-weight: 600;
}

.option-description {
  overflow: hidden;
  font-size: 0.82rem;
  color: var(--color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.panel-message {
  padding: 0.7rem 0.75rem;
  font-size: 0.9rem;
  color: var(--color-muted);
}

.panel-message.error {
  color: var(--color-danger);
}

.powered-by {
  padding: 0.35rem 0.75rem 0.2rem;
  font-size: 0.7rem;
  text-align: right;
  color: var(--color-subtle);
  border-top: 1px solid var(--color-border);
  margin-top: 0.25rem;
}

.selected-place {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-height: var(--control-height);
  padding: 0.55rem 0.5rem 0.55rem 0.75rem;
  background: var(--color-primary-soft);
  border: 1px solid rgb(31 92 86 / 0.25);
  border-radius: var(--radius-sm);
}

.pin {
  flex-shrink: 0;
  color: var(--color-accent);
}

.pin svg {
  display: block;
  width: 1.2rem;
  height: 1.2rem;
}

.selected-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.selected-name {
  font-weight: 700;
  overflow-wrap: anywhere;
}

.selected-description {
  overflow: hidden;
  font-size: 0.8rem;
  color: var(--color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import SelectMenu, { type SelectOption } from '@/components/SelectMenu.vue'
import type { CategoryMeta } from '@/utils/activityDetails'

const props = defineProps<{
  bookingOptions: SelectOption[]
  booking: string
  categories: CategoryMeta[]
  category: string | null
}>()

const emit = defineEmits<{
  'update:booking': [value: string]
  'update:category': [value: string | null]
}>()

const root = ref<HTMLElement | null>(null)
const open = ref(false)

const showBooking = computed(() => props.bookingOptions.length > 2 || props.booking !== '')
const showCategories = computed(() => props.categories.length > 1 || props.category !== null)
const activeCount = computed(() => (props.booking !== '' ? 1 : 0) + (props.category ? 1 : 0))

function onDocumentPointerDown(event: PointerEvent) {
  if (!root.value?.contains(event.target as Node)) {
    open.value = false
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

function clearAll() {
  emit('update:booking', '')
  emit('update:category', null)
}
</script>

<template>
  <div
    v-if="showBooking || showCategories"
    ref="root"
    class="filter-menu"
    @keydown.esc="open = false"
  >
    <button
      type="button"
      class="filter-button"
      :class="{ active: activeCount > 0 }"
      aria-haspopup="dialog"
      :aria-expanded="open"
      data-testid="filter-button"
      @click="open = !open"
    >
      <svg viewBox="0 0 20 20" aria-hidden="true">
        <path
          d="M3.5 5h13M6 10h8m-5.5 5h3"
          fill="none"
          stroke="currentColor"
          stroke-width="1.8"
          stroke-linecap="round"
        />
      </svg>
      Filter
      <span v-if="activeCount > 0" class="count" data-testid="filter-count">{{ activeCount }}</span>
    </button>

    <div
      v-if="open"
      class="popover-menu filter-panel"
      role="dialog"
      aria-label="Filters"
      data-testid="filter-panel"
    >
      <div v-if="showBooking" class="filter-group">
        <span class="group-label">Booking</span>
        <div class="booking-filter" data-testid="booking-filter">
          <SelectMenu
            :model-value="booking"
            :options="bookingOptions"
            name="booking-filter"
            @update:model-value="emit('update:booking', $event)"
          />
        </div>
      </div>

      <div v-if="showCategories" class="filter-group">
        <span class="group-label">Category</span>
        <div class="category-filter" role="group" aria-label="Filter by category">
          <button
            type="button"
            class="filter-chip"
            :aria-pressed="category === null"
            data-testid="filter-all"
            @click="emit('update:category', null)"
          >
            All
          </button>
          <button
            v-for="item in categories"
            :key="item.value"
            type="button"
            class="filter-chip"
            :aria-pressed="category === item.value"
            :data-testid="`filter-${item.value}`"
            @click="emit('update:category', item.value)"
          >
            {{ item.icon }} {{ item.label }}
          </button>
        </div>
      </div>

      <button
        v-if="activeCount > 0"
        type="button"
        class="clear"
        data-testid="clear-filters"
        @click="clearAll"
      >
        Clear filters
      </button>
    </div>
  </div>
</template>

<style scoped>
.filter-menu {
  position: relative;
  flex-shrink: 0;
}

.filter-button {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  height: 2.1rem;
  padding: 0 0.85rem;
  font: inherit;
  font-size: 0.84rem;
  font-weight: 600;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.filter-button svg {
  width: 1rem;
  height: 1rem;
}

.filter-button.active {
  color: var(--color-primary);
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.count {
  display: inline-grid;
  place-items: center;
  min-width: 1.2rem;
  height: 1.2rem;
  font-size: 0.7rem;
  color: #fff;
  background: var(--color-primary);
  border-radius: 999px;
}

.filter-panel {
  position: absolute;
  top: calc(100% + 0.4rem);
  right: 0;
  z-index: 50;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  width: min(22rem, calc(100vw - 2rem));
  padding: 1rem;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.group-label {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.category-filter {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.filter-chip {
  padding: 0.3rem 0.75rem;
  font: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.filter-chip[aria-pressed='true'] {
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-color: var(--color-primary);
}

.clear {
  align-self: flex-start;
  padding: 0;
  font: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--color-primary);
  background: none;
  border: 0;
  cursor: pointer;
}
</style>

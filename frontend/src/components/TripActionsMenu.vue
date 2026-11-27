<script setup lang="ts">
import { ref } from 'vue'
import { useClickOutside } from '@/composables/useClickOutside'

withDefaults(
  defineProps<{
    tripName: string
    disabled?: boolean
    placement?: 'below' | 'side'
  }>(),
  {
    disabled: false,
    placement: 'below',
  },
)

const emit = defineEmits<{
  delete: []
}>()

const root = ref<HTMLElement | null>(null)
const open = ref(false)

useClickOutside(root, open, () => (open.value = false))

function askToDelete() {
  open.value = false
  emit('delete')
}
</script>

<template>
  <div ref="root" class="menu" @keydown.esc="open = false">
    <button
      type="button"
      class="btn btn-ghost btn-small menu-button"
      aria-haspopup="menu"
      :aria-expanded="open"
      :aria-label="`More actions for ${tripName}`"
      data-testid="trip-menu"
      :disabled="disabled"
      @click="open = !open"
    >
      <svg viewBox="0 0 20 20" aria-hidden="true">
        <circle cx="4.5" cy="10" r="1.5" fill="currentColor" />
        <circle cx="10" cy="10" r="1.5" fill="currentColor" />
        <circle cx="15.5" cy="10" r="1.5" fill="currentColor" />
      </svg>
    </button>
    <div v-if="open" class="popover-menu menu-panel" :class="placement" role="menu">
      <button
        type="button"
        class="menu-item danger"
        role="menuitem"
        data-testid="delete-button"
        @click="askToDelete"
      >
        Delete trip
      </button>
    </div>
  </div>
</template>

<style scoped>
.menu {
  position: relative;
  z-index: 1;
  flex-shrink: 0;
}

.menu-button {
  width: 2.25rem;
  padding: 0;
  color: var(--color-subtle);
}

.menu-button svg {
  width: 1.15rem;
  height: 1.15rem;
}

.menu-panel {
  position: absolute;
  top: calc(100% + 0.3rem);
  right: 0;
  min-width: 9.5rem;
  padding: 0.3rem;
}

.menu-panel.side {
  top: 50%;
  right: calc(100% + 0.25rem);
  transform: translateY(-50%);
}

.menu-item {
  width: 100%;
  padding: 0.55rem 0.75rem;
  font: inherit;
  font-size: 0.92rem;
  text-align: left;
  white-space: nowrap;
  background: none;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
}

.menu-item.danger {
  color: var(--color-danger);
}

.menu-item.danger:hover,
.menu-item.danger:focus-visible {
  background: var(--color-danger-soft);
  outline: none;
}
</style>

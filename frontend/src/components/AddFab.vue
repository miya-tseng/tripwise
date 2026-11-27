<script setup lang="ts">
import { ref } from 'vue'
import { useClickOutside } from '@/composables/useClickOutside'

const emit = defineEmits<{
  addActivity: []
  addStay: []
}>()

const root = ref<HTMLElement | null>(null)
const open = ref(false)

useClickOutside(root, open, () => (open.value = false))

function choose(action: 'addActivity' | 'addStay') {
  open.value = false

  if (action === 'addActivity') {
    emit('addActivity')
  } else {
    emit('addStay')
  }
}
</script>

<template>
  <div ref="root" class="fab" :class="{ open: open }" data-testid="fab-area">
    <div v-if="open" class="fab-menu" role="menu" data-testid="fab-menu">
      <button
        type="button"
        role="menuitem"
        class="fab-option"
        data-testid="fab-add-activity"
        @click="choose('addActivity')"
      >
        🗓 Activity
      </button>
      <button
        type="button"
        role="menuitem"
        class="fab-option"
        data-testid="fab-add-stay"
        @click="choose('addStay')"
      >
        🛏 Stay
      </button>
    </div>
    <button
      type="button"
      class="fab-button"
      :aria-expanded="open"
      aria-haspopup="menu"
      :aria-label="open ? 'Close add menu' : 'Add to the trip'"
      data-testid="fab"
      @click="open = !open"
    >
      <span aria-hidden="true">+</span>
    </button>
  </div>
</template>

<style scoped>
.fab {
  position: fixed;
  right: 1rem;
  bottom: calc(5rem + env(safe-area-inset-bottom));
  z-index: 55;
  display: none;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.6rem;
}

.fab-button {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  font-size: 1.9rem;
  line-height: 1;
  color: #fff;
  background: linear-gradient(140deg, #2a6b5f 0%, #1a4f4a 100%);
  border: 0;
  border-radius: 50%;
  box-shadow: 0 10px 24px -8px rgb(31 92 86 / 0.7);
  cursor: pointer;
  transition: transform 0.2s ease;
}

.fab.open .fab-button {
  transform: rotate(45deg);
}

.fab-menu {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.45rem;
}

.fab-option {
  padding: 0.65rem 1rem;
  font: inherit;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  box-shadow: var(--shadow-lg);
  cursor: pointer;
}

@media (max-width: 720px) {
  .fab {
    display: flex;
  }
}
</style>

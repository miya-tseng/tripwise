<script setup lang="ts">
import { nextTick, ref, useId, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    message: string
    confirmLabel?: string
    cancelLabel?: string
  }>(),
  {
    confirmLabel: 'Confirm',
    cancelLabel: 'Cancel',
  },
)

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()

const titleId = useId()
const messageId = useId()
const cancelButton = ref<HTMLButtonElement | null>(null)

watch(
  () => props.open,
  async (open) => {
    if (open) {
      await nextTick()
      cancelButton.value?.focus()
    }
  },
)
</script>

<template>
  <Transition name="dialog">
    <div v-if="open" class="backdrop" data-testid="dialog-backdrop" @click.self="emit('cancel')">
      <div
        class="dialog"
        role="alertdialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        :aria-describedby="messageId"
        @keydown.esc="emit('cancel')"
      >
        <div class="icon" aria-hidden="true">
          <svg viewBox="0 0 24 24">
            <path
              d="M4.5 7h15M10 11v6m4-6v6M6 7l.8 11.2A2 2 0 0 0 8.8 20h6.4a2 2 0 0 0 2-1.8L18 7M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"
              fill="none"
              stroke="currentColor"
              stroke-width="1.6"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </div>
        <h2 :id="titleId" class="title">{{ title }}</h2>
        <p :id="messageId" class="message">{{ message }}</p>
        <div class="actions">
          <button
            ref="cancelButton"
            type="button"
            class="btn btn-secondary"
            data-testid="cancel-button"
            @click="emit('cancel')"
          >
            {{ cancelLabel }}
          </button>
          <button
            type="button"
            class="btn btn-danger"
            data-testid="confirm-button"
            @click="emit('confirm')"
          >
            {{ confirmLabel }}
          </button>
        </div>
      </div>
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
  max-width: 400px;
  padding: 1.75rem;
  text-align: center;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

.icon {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  margin: 0 auto 1rem;
  color: var(--color-danger);
  background: var(--color-danger-soft);
  border-radius: 50%;
}

.icon svg {
  width: 1.6rem;
  height: 1.6rem;
}

.title {
  font-family: var(--font-display);
  font-size: 1.35rem;
  font-weight: 700;
}

.message {
  margin-top: 0.5rem;
  color: var(--color-muted);
}

.actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
  margin-top: 1.5rem;
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
  transform: scale(0.95) translateY(8px);
}
</style>

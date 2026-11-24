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
        <div class="icon" aria-hidden="true">🗑️</div>
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
  background: rgb(15 23 42 / 0.45);
  backdrop-filter: blur(2px);
}

.dialog {
  width: 100%;
  max-width: 400px;
  padding: 1.75rem;
  text-align: center;
  background: var(--color-surface);
  border-radius: 16px;
  box-shadow: 0 20px 50px rgb(0 0 0 / 0.2);
}

.icon {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  margin: 0 auto 1rem;
  font-size: 1.6rem;
  background: var(--color-danger-soft);
  border-radius: 50%;
}

.title {
  font-size: 1.2rem;
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

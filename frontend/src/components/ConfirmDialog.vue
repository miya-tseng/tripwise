<script setup lang="ts">
import BaseDialog from '@/components/BaseDialog.vue'
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
  <BaseDialog
    :open="open"
    role="alertdialog"
    tag="div"
    align="center"
    width="400px"
    :labelledby="titleId"
    :describedby="messageId"
    backdrop-testid="dialog-backdrop"
    @close="emit('cancel')"
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
  </BaseDialog>
</template>

<style scoped>
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
</style>

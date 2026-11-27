<script lang="ts">
const openDialogs: symbol[] = []
</script>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    labelledby: string
    describedby?: string
    role?: 'dialog' | 'alertdialog'
    tag?: 'section' | 'form' | 'div'
    variant?: 'card' | 'sheet'
    align?: 'start' | 'center'
    width?: string
    testid?: string
    backdropTestid?: string
  }>(),
  {
    describedby: undefined,
    role: 'dialog',
    tag: 'section',
    variant: 'card',
    align: 'start',
    width: '460px',
    testid: undefined,
    backdropTestid: undefined,
  },
)

const emit = defineEmits<{
  close: []
  submit: []
}>()

const panel = ref<HTMLElement | null>(null)
const id = Symbol('dialog')

function isTopmost(): boolean {
  return openDialogs.at(-1) === id
}

function onDocumentKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && isTopmost()) {
    event.preventDefault()
    emit('close')
  }
}

function release() {
  const index = openDialogs.indexOf(id)

  if (index >= 0) {
    openDialogs.splice(index, 1)
  }

  document.removeEventListener('keydown', onDocumentKeydown)
}

watch(
  () => props.open,
  async (open) => {
    if (!open) {
      release()
      return
    }

    openDialogs.push(id)
    document.addEventListener('keydown', onDocumentKeydown)
    await nextTick()

    if (panel.value && !panel.value.contains(document.activeElement)) {
      panel.value.focus()
    }
  },
  { immediate: true },
)

onBeforeUnmount(release)

defineExpose({ panel })
</script>

<template>
  <Transition name="dialog">
    <div
      v-if="open"
      class="backdrop"
      :class="variant"
      :data-testid="backdropTestid"
      @click.self="emit('close')"
    >
      <component
        :is="tag"
        ref="panel"
        class="dialog"
        :class="[variant, `align-${align}`]"
        :style="{ maxWidth: width }"
        :role="role"
        aria-modal="true"
        :aria-labelledby="labelledby"
        :aria-describedby="describedby"
        :data-testid="testid"
        tabindex="-1"
        @submit.prevent="emit('submit')"
      >
        <slot />
      </component>
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
  outline: none;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

.dialog.card {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  padding: 1.75rem;
}

.dialog.sheet {
  max-height: calc(100vh - 2rem);
  display: flex;
  flex-direction: column;
}

.dialog.align-center {
  text-align: center;
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
  transform: scale(0.96) translateY(8px);
}

@media (max-width: 560px) {
  .backdrop.sheet {
    place-items: end stretch;
    padding: 0;
  }

  .dialog.sheet {
    max-width: none !important;
    max-height: 92vh;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  }
}
</style>

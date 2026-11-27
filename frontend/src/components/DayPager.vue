<script setup lang="ts">
defineProps<{
  activeIndex: number
  dayCount: number
}>()

const emit = defineEmits<{
  step: [step: number]
}>()
</script>

<template>
  <nav class="day-pager" aria-label="Other days" data-testid="day-pager">
    <button
      type="button"
      class="pager-button"
      :disabled="activeIndex === 0"
      data-testid="previous-day"
      @click="emit('step', -1)"
    >
      <span aria-hidden="true">‹</span>
      {{ activeIndex > 0 ? `Day ${activeIndex}` : 'Start' }}
    </button>
    <span class="pager-hint">Swipe to change day</span>
    <button
      type="button"
      class="pager-button"
      :disabled="activeIndex >= dayCount - 1"
      data-testid="next-day"
      @click="emit('step', 1)"
    >
      {{ activeIndex < dayCount - 1 ? `Day ${activeIndex + 2}` : 'End' }}
      <span aria-hidden="true">›</span>
    </button>
  </nav>
</template>

<style scoped>
.day-pager {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.9rem;
}

.pager-button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  min-height: 2.75rem;
  padding: 0 1rem;
  font: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-primary);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.pager-button span {
  font-size: 1.2rem;
  line-height: 1;
}

.pager-button:disabled {
  color: var(--color-subtle);
  cursor: default;
  opacity: 0.6;
}

.pager-hint {
  font-size: 0.75rem;
  color: var(--color-subtle);
}
</style>

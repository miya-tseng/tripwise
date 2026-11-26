<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { dateParts } from '@/utils/date'

const props = withDefaults(
  defineProps<{
    dates: string[]
    activeIndex?: number
    today?: string | null
  }>(),
  {
    activeIndex: 0,
    today: null,
  },
)

const emit = defineEmits<{
  jump: [index: number]
}>()

const strip = ref<HTMLElement | null>(null)

watch(
  () => props.activeIndex,
  async (index) => {
    await nextTick()
    const chip = strip.value?.children[index] as HTMLElement | undefined
    chip?.scrollIntoView?.({ block: 'nearest', inline: 'center', behavior: 'smooth' })
  },
)
</script>

<template>
  <nav ref="strip" class="day-strip" aria-label="Jump to a day" data-testid="day-strip">
    <button
      v-for="(date, index) in dates"
      :key="date"
      type="button"
      class="day-chip"
      :class="{ active: index === activeIndex, today: date === today }"
      :aria-current="index === activeIndex ? 'true' : undefined"
      :aria-label="`Day ${index + 1}, ${dateParts(date).weekday} ${dateParts(date).month} ${dateParts(date).day}${date === today ? ', today' : ''}`"
      data-testid="day-chip"
      @click="emit('jump', index)"
    >
      <span class="chip-day">D{{ index + 1 }}</span>
      <span class="chip-date">{{ dateParts(date).month }} {{ dateParts(date).day }}</span>
    </button>
  </nav>
</template>

<style scoped>
.day-strip {
  display: flex;
  gap: 0.35rem;
  overflow-x: auto;
  scrollbar-width: none;
  scroll-snap-type: x proximity;
}

.day-strip::-webkit-scrollbar {
  display: none;
}

.day-chip {
  position: relative;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 3.4rem;
  padding: 0.25rem 0.55rem;
  font: inherit;
  line-height: 1.15;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 10px;
  cursor: pointer;
  scroll-snap-align: center;
}

.chip-day {
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--color-text);
}

.chip-date {
  font-size: 0.66rem;
  white-space: nowrap;
}

.day-chip.today::after {
  content: '';
  position: absolute;
  top: 0.25rem;
  right: 0.3rem;
  width: 0.4rem;
  height: 0.4rem;
  background: var(--color-accent);
  border-radius: 50%;
}

.day-chip.active {
  color: rgb(255 255 255 / 0.85);
  background: var(--color-primary);
  border-color: var(--color-primary);
}

.day-chip.active .chip-day {
  color: #fff;
}
</style>

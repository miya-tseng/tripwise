<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import TripCard from '@/components/TripCard.vue'
import type { Trip } from '@/types/trip'
import type { TripPhase } from '@/utils/trips'

const props = withDefaults(
  defineProps<{
    trips: Trip[]
    phase: TripPhase
    today: string
    deletingId?: string | null
  }>(),
  {
    deletingId: null,
  },
)

const emit = defineEmits<{
  delete: [trip: Trip]
}>()

const row = ref<HTMLElement | null>(null)
const canScrollBack = ref(false)
const canScrollForward = ref(false)

function updateScrollState() {
  const element = row.value

  if (!element) {
    return
  }

  canScrollBack.value = element.scrollLeft > 4
  canScrollForward.value = element.scrollLeft + element.clientWidth < element.scrollWidth - 4
}

function scroll(step: number) {
  const element = row.value

  if (element) {
    element.scrollBy?.({ left: step * element.clientWidth * 0.9, behavior: 'smooth' })
  }
}

let resizeObserver: ResizeObserver | undefined

onMounted(() => {
  updateScrollState()

  if (typeof ResizeObserver !== 'undefined' && row.value) {
    resizeObserver = new ResizeObserver(updateScrollState)
    resizeObserver.observe(row.value)
  }
})

onBeforeUnmount(() => resizeObserver?.disconnect())

watch(
  () => props.trips.length,
  async () => {
    await nextTick()
    updateScrollState()
  },
)

defineExpose({ scroll, canScrollBack, canScrollForward })
</script>

<template>
  <ul
    ref="row"
    class="trip-list"
    :class="phase"
    :data-testid="`${phase}-trips`"
    @scroll.passive="updateScrollState"
  >
    <TripCard
      v-for="trip in trips"
      :key="trip.id"
      :trip="trip"
      :phase="phase"
      :today="today"
      :deleting="deletingId === trip.id"
      @delete="emit('delete', $event)"
    />
  </ul>
</template>

<style scoped>
.trip-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 1rem;
}

.trip-list.upcoming,
.trip-list.past {
  grid-auto-flow: column;
  grid-auto-columns: minmax(18rem, calc((100% - 2rem) / 3));
  margin: -0.5rem -0.5rem 0;
  padding: 0.5rem 0.5rem 0.9rem;
  overflow-x: auto;
  overscroll-behavior-x: contain;
  scroll-padding-inline: 0.5rem;
  scroll-snap-type: x mandatory;
  scrollbar-width: thin;
}

.trip-list.past {
  grid-auto-columns: minmax(16rem, calc((100% - 3 * 0.6rem) / 4));
  gap: 0.6rem;
}

.trip-list.upcoming > :deep(*),
.trip-list.past > :deep(*) {
  scroll-snap-align: start;
}

@media (max-width: 720px) {
  .trip-list.upcoming,
  .trip-list.past {
    grid-auto-columns: 82%;
    margin-inline: -1.25rem;
    padding-inline: 1.25rem;
    scroll-padding-inline: 1.25rem;
    scrollbar-width: none;
  }

  .trip-list.upcoming::-webkit-scrollbar,
  .trip-list.past::-webkit-scrollbar {
    display: none;
  }
}
</style>

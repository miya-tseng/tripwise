<script setup lang="ts">
import TripCard from '@/components/TripCard.vue'
import type { Trip } from '@/types/trip'
import type { TripPhase } from '@/utils/trips'

withDefaults(
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
</script>

<template>
  <ul class="trip-list" :class="phase" :data-testid="`${phase}-trips`">
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

.trip-list.upcoming {
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 440px), 1fr));
}

.trip-list.past {
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 320px), 1fr));
  gap: 0.6rem;
}
</style>

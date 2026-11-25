<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink } from 'vue-router'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { Trip } from '@/types/trip'
import { dateParts, formatDateRange } from '@/utils/date'

withDefaults(
  defineProps<{
    trips: Trip[]
    deletingId?: string | null
  }>(),
  {
    deletingId: null,
  },
)

const emit = defineEmits<{
  delete: [id: string]
}>()

const tripToDelete = ref<Trip | null>(null)

const confirmMessage = computed(() =>
  tripToDelete.value
    ? `Are you sure you want to delete "${tripToDelete.value.name}"? This cannot be undone.`
    : '',
)

function askToDelete(trip: Trip) {
  tripToDelete.value = trip
}

function confirmDelete() {
  if (tripToDelete.value) {
    emit('delete', tripToDelete.value.id)
  }
  tripToDelete.value = null
}

function cancelDelete() {
  tripToDelete.value = null
}
</script>

<template>
  <div v-if="trips.length === 0" class="empty">
    <span class="empty-icon" aria-hidden="true">
      <svg viewBox="0 0 24 24">
        <path
          d="M8 7V5.5A1.5 1.5 0 0 1 9.5 4h5A1.5 1.5 0 0 1 16 5.5V7m-11 0h14a1 1 0 0 1 1 1v10a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a1 1 0 0 1 1-1Zm4 0v13m6-13v13"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </span>
    <p data-testid="empty-state">No trips yet.</p>
    <p class="empty-hint">Create your first trip above to start planning.</p>
  </div>
  <ul v-else class="trip-list">
    <li v-for="trip in trips" :key="trip.id" class="card trip" data-testid="trip-item">
      <div class="date-tile" aria-hidden="true">
        <span class="tile-month">{{ dateParts(trip.startDate).month }}</span>
        <span class="tile-day">{{ dateParts(trip.startDate).day }}</span>
      </div>
      <div class="trip-info">
        <RouterLink
          :to="{ name: 'trip-schedule', params: { id: trip.id } }"
          class="trip-name"
          data-testid="trip-link"
        >
          {{ trip.name }}
        </RouterLink>
        <span class="trip-dates">{{ formatDateRange(trip.startDate, trip.endDate) }}</span>
      </div>
      <span class="chip">{{ trip.days }} {{ trip.days === 1 ? 'day' : 'days' }}</span>
      <button
        type="button"
        class="btn btn-ghost btn-small delete"
        data-testid="delete-button"
        :disabled="deletingId === trip.id"
        @click="askToDelete(trip)"
      >
        {{ deletingId === trip.id ? 'Deleting...' : 'Delete' }}
      </button>
      <svg class="arrow" viewBox="0 0 20 20" aria-hidden="true">
        <path
          d="m8 5 5 5-5 5"
          fill="none"
          stroke="currentColor"
          stroke-width="1.7"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </li>
  </ul>

  <ConfirmDialog
    :open="tripToDelete !== null"
    title="Delete trip?"
    :message="confirmMessage"
    confirm-label="Delete"
    @confirm="confirmDelete"
    @cancel="cancelDelete"
  />
</template>

<style scoped>
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  padding: 3rem 1rem;
  color: var(--color-muted);
  background: rgb(255 255 255 / 0.5);
  border: 1.5px dashed var(--color-border-strong);
  border-radius: var(--radius);
}

.empty-icon {
  display: grid;
  place-items: center;
  width: 3rem;
  height: 3rem;
  margin-bottom: 0.4rem;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 50%;
}

.empty-icon svg {
  width: 1.5rem;
  height: 1.5rem;
}

.empty p:first-of-type {
  font-weight: 600;
  color: var(--color-text);
}

.empty-hint {
  font-size: 0.9rem;
}

.trip-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 440px), 1fr));
  gap: 1rem;
}

.trip {
  position: relative;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1rem 1rem 1.1rem;
  cursor: pointer;
  transition:
    transform 0.18s ease,
    box-shadow 0.18s ease,
    border-color 0.18s ease;
}

.trip:hover {
  transform: translateY(-2px);
  border-color: rgb(31 92 86 / 0.3);
  box-shadow: var(--shadow-lg);
}

.trip:has(.trip-name:focus-visible) {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 4px var(--color-primary-ring);
}

.date-tile {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 3.6rem;
  height: 3.8rem;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  border-radius: 12px;
  line-height: 1;
}

.tile-month {
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.tile-day {
  margin-top: 0.2rem;
  font-family: var(--font-display);
  font-size: 1.6rem;
  font-weight: 700;
}

.trip-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}

.trip-name {
  overflow: hidden;
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: -0.01em;
  color: var(--color-text);
  text-decoration: none;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: color 0.15s;
}

.trip-name::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
}

.trip-name:focus-visible {
  outline: none;
}

.trip:hover .trip-name {
  color: var(--color-primary);
}

.trip-dates {
  font-size: 0.9rem;
  color: var(--color-muted);
}

.delete {
  position: relative;
  z-index: 1;
  min-width: 4.5rem;
}

.delete:hover:not(:disabled) {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}

.arrow {
  flex-shrink: 0;
  width: 1.1rem;
  height: 1.1rem;
  margin-left: -0.35rem;
  color: var(--color-subtle);
  transition:
    transform 0.18s ease,
    color 0.18s ease;
}

.trip:hover .arrow {
  color: var(--color-primary);
  transform: translateX(3px);
}
</style>

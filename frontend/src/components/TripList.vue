<script setup lang="ts">
import { computed, ref } from 'vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { Trip } from '@/types/trip'
import { formatDateRange } from '@/utils/date'

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
    <span class="empty-icon" aria-hidden="true">🧳</span>
    <p data-testid="empty-state">No trips yet.</p>
  </div>
  <ul v-else class="trip-list">
    <li v-for="trip in trips" :key="trip.id" class="card trip" data-testid="trip-item">
      <div class="trip-info">
        <strong class="trip-name">{{ trip.name }}</strong>
        <span class="trip-dates">{{ formatDateRange(trip.startDate, trip.endDate) }}</span>
      </div>
      <span class="badge">{{ trip.days }} {{ trip.days === 1 ? 'day' : 'days' }}</span>
      <button
        type="button"
        class="btn btn-danger-outline"
        data-testid="delete-button"
        :disabled="deletingId === trip.id"
        @click="askToDelete(trip)"
      >
        {{ deletingId === trip.id ? 'Deleting...' : 'Delete' }}
      </button>
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
  gap: 0.5rem;
  padding: 2.5rem 1rem;
  color: var(--color-muted);
  border: 2px dashed var(--color-border);
  border-radius: var(--radius);
}

.empty-icon {
  font-size: 2rem;
}

.trip-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.trip {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.25rem;
  transition:
    transform 0.15s,
    box-shadow 0.15s;
}

.trip:hover {
  transform: translateY(-1px);
  box-shadow:
    0 1px 2px rgb(0 0 0 / 0.04),
    0 8px 24px rgb(0 0 0 / 0.08);
}

.trip-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.trip-name {
  font-size: 1.05rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trip-dates {
  font-size: 0.9rem;
  color: var(--color-muted);
}

.badge {
  padding: 0.2rem 0.7rem;
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 999px;
  white-space: nowrap;
}

.btn-danger-outline {
  min-width: 6.5rem;
}
</style>

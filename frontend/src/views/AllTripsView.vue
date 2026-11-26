<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import AppBar from '@/components/AppBar.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import TripRow from '@/components/TripRow.vue'
import { useTripsStore } from '@/stores/trips'
import type { Trip } from '@/types/trip'
import { groupByMonth, groupTrips, searchTrips, todayIso } from '@/utils/trips'

type Tab = 'upcoming' | 'past'

const props = withDefaults(defineProps<{ today?: string }>(), { today: () => todayIso() })

const store = useTripsStore()
const { trips, loading, error, deletingId } = storeToRefs(store)

const query = ref('')
const tab = ref<Tab>('upcoming')
const tripToDelete = ref<Trip | null>(null)

const groups = computed(() => groupTrips(trips.value, props.today))
const upcoming = computed(() => [...groups.value.ongoing, ...groups.value.upcoming])
const tabTrips = computed(() => (tab.value === 'upcoming' ? upcoming.value : groups.value.past))
const results = computed(() => searchTrips(tabTrips.value, query.value))
const months = computed(() => groupByMonth(results.value))
const confirmMessage = computed(() =>
  tripToDelete.value
    ? `Are you sure you want to delete "${tripToDelete.value.name}"? This cannot be undone.`
    : '',
)

onMounted(() => {
  store.load()
})

function confirmDelete() {
  if (tripToDelete.value) {
    store.remove(tripToDelete.value.id)
  }
  tripToDelete.value = null
}
</script>

<template>
  <AppBar />
  <main class="page">
    <header class="head">
      <h1 class="display">All trips</h1>
      <label class="search">
        <svg viewBox="0 0 20 20" aria-hidden="true">
          <circle cx="9" cy="9" r="5.5" fill="none" stroke="currentColor" stroke-width="1.7" />
          <path
            d="m13.2 13.2 3.3 3.3"
            stroke="currentColor"
            stroke-width="1.7"
            stroke-linecap="round"
          />
        </svg>
        <input
          v-model="query"
          class="text-input"
          type="search"
          placeholder="Search by name or destination"
          aria-label="Search trips"
          data-testid="trip-search"
        />
      </label>
      <div class="tabs" role="tablist" aria-label="Trips">
        <button
          type="button"
          role="tab"
          class="tab"
          :aria-selected="tab === 'upcoming'"
          data-testid="tab-upcoming"
          @click="tab = 'upcoming'"
        >
          Upcoming <span class="tab-count">{{ upcoming.length }}</span>
        </button>
        <button
          type="button"
          role="tab"
          class="tab"
          :aria-selected="tab === 'past'"
          data-testid="tab-past"
          @click="tab = 'past'"
        >
          Past <span class="tab-count">{{ groups.past.length }}</span>
        </button>
      </div>
    </header>

    <div v-if="error" role="alert" class="alert">
      <span>{{ error }}</span>
      <button
        type="button"
        class="alert-close"
        aria-label="Dismiss"
        data-testid="dismiss-error"
        @click="store.clearError"
      >
        ✕
      </button>
    </div>

    <p v-if="loading" class="muted">Loading...</p>
    <p v-else-if="results.length === 0" class="empty" data-testid="no-trips">
      {{
        query.trim()
          ? `No trips match "${query.trim()}".`
          : tab === 'upcoming'
            ? 'No upcoming trips yet.'
            : 'No past trips yet.'
      }}
    </p>
    <div v-else class="months">
      <section
        v-for="month in months"
        :key="month.key"
        class="month card"
        :aria-label="month.label"
        data-testid="trip-month"
      >
        <h2 class="month-label">{{ month.label }}</h2>
        <ul class="rows">
          <TripRow
            v-for="trip in month.trips"
            :key="trip.id"
            :trip="trip"
            :today="today"
            :deleting="deletingId === trip.id"
            @delete="tripToDelete = $event"
          />
        </ul>
      </section>
    </div>

    <ConfirmDialog
      :open="tripToDelete !== null"
      title="Delete trip?"
      :message="confirmMessage"
      confirm-label="Delete"
      @confirm="confirmDelete"
      @cancel="tripToDelete = null"
    />
  </main>
</template>

<style scoped>
.page {
  max-width: 760px;
  margin: 0 auto;
  padding: 1.75rem clamp(1rem, 3.5vw, 2rem) 4rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.head {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.head h1 {
  font-size: 2rem;
  line-height: 1.1;
}

.search {
  position: relative;
  display: block;
}

.search svg {
  position: absolute;
  top: 50%;
  left: 0.85rem;
  width: 1.1rem;
  height: 1.1rem;
  color: var(--color-subtle);
  transform: translateY(-50%);
}

.search input {
  padding-left: 2.5rem;
}

.tabs {
  display: inline-flex;
  align-self: flex-start;
  padding: 0.2rem;
  background: rgb(27 36 49 / 0.06);
  border-radius: 10px;
}

.tab {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.9rem;
  font: inherit;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--color-muted);
  background: none;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
}

.tab[aria-selected='true'] {
  color: var(--color-text);
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
}

.tab-count {
  font-size: 0.75rem;
  color: var(--color-subtle);
}

.months {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.month {
  padding: 0.75rem 0.6rem 0.5rem;
}

.month-label {
  padding: 0 0.6rem 0.25rem;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.rows {
  margin: 0;
  padding: 0;
  list-style: none;
}

.empty {
  padding: 2.5rem 1rem;
  text-align: center;
  color: var(--color-muted);
  background: rgb(255 255 255 / 0.5);
  border: 1.5px dashed var(--color-border-strong);
  border-radius: var(--radius);
}

.muted {
  color: var(--color-muted);
}
</style>

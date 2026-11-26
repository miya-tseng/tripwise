<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import NewTripDialog from '@/components/NewTripDialog.vue'
import TripList from '@/components/TripList.vue'
import { useTripsStore } from '@/stores/trips'
import type { CreateTripInput, Trip } from '@/types/trip'
import { groupTrips, todayIso } from '@/utils/trips'

const props = withDefaults(defineProps<{ today?: string }>(), { today: () => todayIso() })

const store = useTripsStore()
const { trips, loading, error, deletingId } = storeToRefs(store)

const newTripOpen = ref(false)
const pastOpen = ref(false)
const tripToDelete = ref<Trip | null>(null)

const groups = computed(() => groupTrips(trips.value, props.today))
const hasTrips = computed(() => trips.value.length > 0)
const confirmMessage = computed(() =>
  tripToDelete.value
    ? `Are you sure you want to delete "${tripToDelete.value.name}"? This cannot be undone.`
    : '',
)

onMounted(() => {
  store.load()
})

function openNewTrip() {
  store.clearError()
  newTripOpen.value = true
}

function closeNewTrip() {
  newTripOpen.value = false
  store.clearError()
}

async function createTrip(input: CreateTripInput): Promise<boolean> {
  return store.create(input)
}

function confirmDelete() {
  if (tripToDelete.value) {
    store.remove(tripToDelete.value.id)
  }
  tripToDelete.value = null
}
</script>

<template>
  <main class="page">
    <header class="hero">
      <span class="logo" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <path
            d="M10.2 13.8 3.5 11l1.4-1.4 7.3.9 4.2-4.2c.8-.8 2.1-.9 2.8-.2.7.7.6 2-.2 2.8l-4.2 4.2.9 7.3-1.4 1.4-2.8-6.7-3 3 .3 2.4-1 1-1.6-3.1-3.1-1.6 1-1 2.4.3 3-3Z"
            fill="currentColor"
          />
        </svg>
      </span>
      <div class="hero-text">
        <h1 class="display">TripWise</h1>
        <p class="tagline">Plan your trips day by day.</p>
      </div>
      <button
        v-if="hasTrips"
        type="button"
        class="btn btn-primary new-trip"
        data-testid="new-trip"
        @click="openNewTrip"
      >
        <span aria-hidden="true">+</span> New trip
      </button>
    </header>

    <div v-if="error && !newTripOpen" role="alert" class="alert">
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

    <p v-if="loading" class="loading">Loading...</p>

    <div v-else-if="!hasTrips" class="empty">
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
      <p class="empty-title" data-testid="empty-state">No trips yet.</p>
      <p class="empty-hint">Pick a destination and dates to start planning.</p>
      <button type="button" class="btn btn-primary" data-testid="new-trip" @click="openNewTrip">
        Plan your first trip
      </button>
    </div>

    <template v-else>
      <section
        v-if="groups.ongoing.length > 0"
        class="group"
        aria-label="Happening now"
        data-testid="ongoing-section"
      >
        <TripList
          :trips="groups.ongoing"
          phase="ongoing"
          :today="today"
          :deleting-id="deletingId"
          @delete="tripToDelete = $event"
        />
      </section>

      <section class="group" data-testid="upcoming-section">
        <div class="section-header">
          <h2 class="section-title">Upcoming</h2>
          <span class="chip">{{ groups.upcoming.length }}</span>
        </div>
        <TripList
          v-if="groups.upcoming.length > 0"
          :trips="groups.upcoming"
          phase="upcoming"
          :today="today"
          :deleting-id="deletingId"
          @delete="tripToDelete = $event"
        />
        <button
          v-else
          type="button"
          class="add-upcoming"
          data-testid="add-upcoming"
          @click="openNewTrip"
        >
          + Plan your next trip
        </button>
      </section>

      <section v-if="groups.past.length > 0" class="group past" data-testid="past-section">
        <button
          type="button"
          class="past-toggle"
          :aria-expanded="pastOpen"
          data-testid="toggle-past"
          @click="pastOpen = !pastOpen"
        >
          <svg class="caret" :class="{ open: pastOpen }" viewBox="0 0 20 20" aria-hidden="true">
            <path
              d="m8 5 5 5-5 5"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
          <span class="section-title">Past trips</span>
          <span class="chip">{{ groups.past.length }}</span>
        </button>
        <TripList
          v-if="pastOpen"
          :trips="groups.past"
          phase="past"
          :today="today"
          :deleting-id="deletingId"
          @delete="tripToDelete = $event"
        />
      </section>
    </template>

    <NewTripDialog :open="newTripOpen" :submit="createTrip" :error="error" @close="closeNewTrip" />

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
  max-width: 1040px;
  margin: 0 auto;
  padding: 3rem clamp(1.25rem, 3.5vw, 2.75rem) 4.5rem;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.hero {
  display: flex;
  align-items: center;
  gap: 1.1rem;
}

.hero-text {
  flex: 1;
  min-width: 0;
}

.logo {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  color: #fff;
  background: linear-gradient(140deg, #2a6b5f 0%, #1a4f4a 100%);
  border-radius: 16px;
  box-shadow:
    inset 0 1px 0 rgb(255 255 255 / 0.15),
    0 10px 24px -10px rgb(31 92 86 / 0.7);
}

.logo svg {
  width: 1.7rem;
  height: 1.7rem;
  transform: rotate(-8deg);
}

h1 {
  font-size: 2.4rem;
  line-height: 1.1;
}

.tagline {
  margin-top: 0.15rem;
  color: var(--color-muted);
}

.new-trip {
  flex-shrink: 0;
}

.group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.add-upcoming {
  padding: 1.1rem;
  font: inherit;
  font-weight: 600;
  color: var(--color-primary);
  background: rgb(255 255 255 / 0.5);
  border: 1.5px dashed var(--color-border-strong);
  border-radius: var(--radius);
  cursor: pointer;
}

.add-upcoming:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.past-toggle {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  align-self: flex-start;
  padding: 0.25rem 0.5rem 0.25rem 0.15rem;
  font: inherit;
  color: var(--color-muted);
  background: none;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
}

.past-toggle .section-title {
  color: var(--color-muted);
}

.past-toggle:hover .section-title {
  color: var(--color-text);
}

.caret {
  flex-shrink: 0;
  width: 1.15rem;
  height: 1.15rem;
  transition: transform 0.18s ease;
}

.caret.open {
  transform: rotate(90deg);
}

.loading {
  color: var(--color-muted);
}

.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  padding: 3.5rem 1rem;
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

.empty-title {
  font-weight: 600;
  color: var(--color-text);
}

.empty-hint {
  margin-bottom: 0.9rem;
  font-size: 0.9rem;
}

@media (max-width: 560px) {
  .hero {
    flex-wrap: wrap;
  }

  .new-trip {
    width: 100%;
  }
}
</style>

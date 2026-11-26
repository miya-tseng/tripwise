<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import AppBar from '@/components/AppBar.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import NewTripDialog from '@/components/NewTripDialog.vue'
import TripList from '@/components/TripList.vue'
import { useTripsStore } from '@/stores/trips'
import type { CreateTripInput, Trip } from '@/types/trip'
import { dateParts } from '@/utils/date'
import { greeting, tripsSummary } from '@/utils/tripTheme'
import { groupTrips, todayIso } from '@/utils/trips'

const props = withDefaults(defineProps<{ today?: string }>(), { today: () => todayIso() })

const store = useTripsStore()
const { trips, loading, error, deletingId } = storeToRefs(store)

const newTripOpen = ref(false)
const pastOpen = ref(false)
const tripToDelete = ref<Trip | null>(null)

const groups = computed(() => groupTrips(trips.value, props.today))
const hasTrips = computed(() => trips.value.length > 0)
const greetingText = greeting()
const summary = computed(() => tripsSummary(groups.value))
const todayLabel = computed(() => {
  const { weekday, month, day } = dateParts(props.today)
  return `${weekday}, ${month} ${day}`
})
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
  <AppBar>
    <button
      v-if="hasTrips"
      type="button"
      class="btn btn-primary new-trip"
      aria-label="New trip"
      data-testid="new-trip"
      @click="openNewTrip"
    >
      <span class="new-trip-plus" aria-hidden="true">+</span>
      <span class="new-trip-label">New trip</span>
    </button>
  </AppBar>
  <main class="page">
    <section class="hero" aria-labelledby="greeting">
      <svg class="hero-route" viewBox="0 0 600 160" aria-hidden="true">
        <path
          d="M10 130 C 120 40, 220 150, 330 80 S 520 20, 590 60"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-dasharray="2 9"
          stroke-linecap="round"
        />
        <circle cx="10" cy="130" r="5" fill="currentColor" />
        <circle cx="590" cy="60" r="7" fill="none" stroke="currentColor" stroke-width="2" />
      </svg>
      <p class="eyebrow today" data-testid="today-label">{{ todayLabel }}</p>
      <h1 id="greeting" class="display">{{ greetingText }}</h1>
      <p class="tagline" data-testid="trips-summary">{{ summary }}</p>
    </section>

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
  padding: 2rem clamp(1.25rem, 3.5vw, 2.75rem) 4.5rem;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.hero {
  position: relative;
  padding: 0.5rem 0 0.25rem;
}

.hero-route {
  position: absolute;
  top: -0.5rem;
  right: 0;
  width: min(60%, 560px);
  height: auto;
  color: rgb(31 92 86 / 0.18);
  pointer-events: none;
}

.today {
  position: relative;
}

h1 {
  position: relative;
  margin-top: 0.2rem;
  font-size: clamp(2rem, 5vw, 2.8rem);
  line-height: 1.1;
}

.tagline {
  position: relative;
  margin-top: 0.35rem;
  font-size: 1.02rem;
  color: var(--color-muted);
}

.new-trip {
  flex-shrink: 0;
  gap: 0.35rem;
}

.new-trip-plus {
  font-size: 1.15rem;
  line-height: 1;
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
  .new-trip {
    width: 2.6rem;
    height: 2.6rem;
    padding: 0;
    border-radius: 50%;
  }

  .new-trip-label {
    display: none;
  }

  .new-trip-plus {
    font-size: 1.5rem;
  }

  .hero-route {
    width: 85%;
    opacity: 0.7;
  }
}
</style>

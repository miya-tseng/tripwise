<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import TripForm from '@/components/TripForm.vue'
import TripList from '@/components/TripList.vue'
import { useTripsStore } from '@/stores/trips'

const store = useTripsStore()
const { trips, loading, error, deletingId } = storeToRefs(store)

onMounted(() => {
  store.load()
})
</script>

<template>
  <main class="page">
    <header class="hero">
      <span class="logo" aria-hidden="true">✈️</span>
      <div>
        <h1>TripWise</h1>
        <p class="tagline">Plan your trips day by day.</p>
      </div>
    </header>

    <section class="card">
      <h2 class="section-title">New trip</h2>
      <TripForm :submit="store.create" />
    </section>

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

    <section class="trips">
      <div class="section-header">
        <h2 class="section-title">Your trips</h2>
        <span v-if="!loading" class="count">{{ trips.length }}</span>
      </div>
      <p v-if="loading" class="loading">Loading...</p>
      <TripList v-else :trips="trips" :deleting-id="deletingId" @delete="store.remove" />
    </section>
  </main>
</template>

<style scoped>
.page {
  max-width: 760px;
  margin: 0 auto;
  padding: 3rem 1.25rem 4rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.hero {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.logo {
  display: grid;
  place-items: center;
  width: 3.25rem;
  height: 3.25rem;
  font-size: 1.6rem;
  background: var(--color-primary-soft);
  border-radius: 14px;
}

h1 {
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.tagline {
  color: var(--color-muted);
}

.section-title {
  font-size: 1.1rem;
  font-weight: 700;
}

.card .section-title {
  margin-bottom: 1rem;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.count {
  min-width: 1.6rem;
  padding: 0 0.5rem;
  font-size: 0.8rem;
  font-weight: 700;
  line-height: 1.6rem;
  text-align: center;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 999px;
}

.alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.85rem 1rem;
  color: var(--color-danger);
  background: var(--color-danger-soft);
  border: 1px solid #fecaca;
  border-radius: 8px;
  font-weight: 500;
}

.alert-close {
  flex-shrink: 0;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  color: var(--color-danger);
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.alert-close:hover {
  background: #fee2e2;
}

.loading {
  color: var(--color-muted);
}
</style>

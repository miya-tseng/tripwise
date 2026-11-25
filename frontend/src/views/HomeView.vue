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
      <span class="logo" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <path
            d="M10.2 13.8 3.5 11l1.4-1.4 7.3.9 4.2-4.2c.8-.8 2.1-.9 2.8-.2.7.7.6 2-.2 2.8l-4.2 4.2.9 7.3-1.4 1.4-2.8-6.7-3 3 .3 2.4-1 1-1.6-3.1-3.1-1.6 1-1 2.4.3 3-3Z"
            fill="currentColor"
          />
        </svg>
      </span>
      <div>
        <h1 class="display">TripWise</h1>
        <p class="tagline">Plan your trips day by day.</p>
      </div>
    </header>

    <section class="card new-trip">
      <span class="eyebrow">Start planning</span>
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
        <span v-if="!loading" class="chip">{{ trips.length }}</span>
      </div>
      <p v-if="loading" class="loading">Loading...</p>
      <TripList v-else :trips="trips" :deleting-id="deletingId" @delete="store.remove" />
    </section>
  </main>
</template>

<style scoped>
.page {
  max-width: 1040px;
  margin: 0 auto;
  padding: 3rem clamp(1.25rem, 3.5vw, 2.75rem) 4.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
}

.hero {
  display: flex;
  align-items: center;
  gap: 1.1rem;
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
  padding: 1.6rem 1.75rem 1.75rem;
}

.new-trip .section-title {
  margin: 0.2rem 0 1.2rem;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 1rem;
}

.loading {
  color: var(--color-muted);
}
</style>

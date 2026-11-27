<script setup lang="ts">
import { ref } from 'vue'
import type { Stay } from '@/types/stay'
import { bookingStatusMeta } from '@/utils/activityDetails'
import { formatCurrency } from '@/utils/money'
import { nightsLabel, stayRangeLabel } from '@/utils/stays'

defineProps<{
  stays: Stay[]
  nightsWithoutStay: number
}>()

const emit = defineEmits<{
  add: []
  edit: [stay: Stay]
}>()

const staysOpen = ref(false)
</script>

<template>
  <section class="stays" aria-label="Stays" data-testid="stays-section">
    <div class="stays-head">
      <h2 class="stays-title">Stays</h2>
      <button
        v-if="stays.length > 0"
        type="button"
        class="stays-toggle"
        :aria-expanded="staysOpen"
        data-testid="stays-toggle"
        @click="staysOpen = !staysOpen"
      >
        🛏 {{ stays.length }} {{ stays.length === 1 ? 'stay' : 'stays' }}
        <span class="stays-toggle-hint">{{ staysOpen ? 'Hide' : 'Show' }}</span>
      </button>
      <span v-if="nightsWithoutStay > 0" class="stays-hint" data-testid="nights-without-stay">
        {{ nightsLabel(nightsWithoutStay) }} without a stay
      </span>
      <button
        type="button"
        class="btn btn-secondary btn-small add-stay"
        data-testid="add-stay"
        @click="emit('add')"
      >
        + Add stay
      </button>
    </div>
    <ul v-if="stays.length > 0" class="stay-cards" :class="{ collapsed: !staysOpen }">
      <li v-for="stay in stays" :key="stay.id">
        <button type="button" class="stay-card" data-testid="stay-card" @click="emit('edit', stay)">
          <span class="stay-card-name" data-testid="stay-card-name">🛏 {{ stay.name }}</span>
          <span class="stay-card-dates">
            {{ stayRangeLabel(stay) }} · {{ nightsLabel(stay.nights) }}
          </span>
          <span class="stay-card-foot">
            <span v-if="stay.cost > 0" data-testid="stay-card-cost">{{
              formatCurrency(stay.cost, stay.currency)
            }}</span>
            <span
              v-if="stay.bookingStatus !== 'NotNeeded'"
              class="booking-badge"
              :class="`tone-${bookingStatusMeta(stay.bookingStatus).tone}`"
            >
              {{ bookingStatusMeta(stay.bookingStatus).label }}
            </span>
          </span>
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.stays {
  margin-bottom: 1.25rem;
}

.stays-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.6rem;
}

.stays-title {
  font-size: 1rem;
  font-weight: 700;
}

.stays-hint {
  font-size: 0.82rem;
  color: var(--color-muted);
}

.stays-head .btn {
  margin-left: auto;
}

.stay-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
  gap: 0.6rem;
  padding: 0;
  list-style: none;
}

.stay-card {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  width: 100%;
  padding: 0.75rem 0.9rem;
  font: inherit;
  text-align: left;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  cursor: pointer;
}

.stay-card:hover {
  border-color: var(--color-primary);
}

.stay-card-name {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.stay-card-dates {
  font-size: 0.82rem;
  color: var(--color-muted);
}

.stay-card-foot {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  font-variant-numeric: tabular-nums;
}

.stay-cards.collapsed {
  display: none;
}

.stays-title {
  display: none;
}

.stays-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  align-self: flex-start;
  padding: 0.45rem 0.8rem;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
}

.stays-toggle-hint {
  font-size: 0.78rem;
  color: var(--color-primary);
}

@media (max-width: 720px) {
  .add-stay {
    display: none;
  }

  .stays {
    margin-bottom: 0.9rem;
  }
}
</style>

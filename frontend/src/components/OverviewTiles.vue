<script setup lang="ts">
import { computed } from 'vue'
import type { TripOverview } from '@/types/overview'
import { formatTwd } from '@/utils/money'
import { nightsLabel } from '@/utils/stays'

const props = defineProps<{
  overview: TripOverview
}>()

const booking = computed(() => {
  const counts = props.overview.booking
  const done = counts.booked + counts.paid
  const needing = done + counts.toBook
  return { ...counts, done, needing, percent: needing > 0 ? Math.round((done / needing) * 100) : 0 }
})

function twd(amount: number | null): string {
  return amount === null ? '—' : formatTwd(amount)
}
</script>

<template>
  <section class="tiles" aria-label="Key facts">
    <div class="tile hero" data-testid="total-spend">
      <span class="tile-label">Total spend</span>
      <span class="tile-value">{{ twd(overview.totalInTwd) }}</span>
      <span class="tile-hint">{{ twd(overview.averagePerDayInTwd) }} per day</span>
    </div>
    <div class="tile" data-testid="activity-total">
      <span class="tile-label">Activities</span>
      <span class="tile-value">{{ overview.activityCount }}</span>
      <span class="tile-hint">
        {{ overview.placeCount }} {{ overview.placeCount === 1 ? 'place' : 'places' }}
      </span>
    </div>
    <div class="tile" data-testid="nights-covered">
      <span class="tile-label">Nights with a stay</span>
      <span class="tile-value">{{ overview.nightsWithStay }} / {{ overview.nights }}</span>
      <span class="tile-hint">
        {{
          overview.nights - overview.nightsWithStay === 0
            ? 'Every night is covered'
            : `${nightsLabel(overview.nights - overview.nightsWithStay)} to go`
        }}
      </span>
    </div>
    <div class="tile" data-testid="booking-progress">
      <span class="tile-label">Booked</span>
      <span class="tile-value">{{ booking.done }} / {{ booking.needing }}</span>
      <div
        class="meter"
        role="meter"
        aria-label="Booking progress"
        :aria-valuenow="booking.done"
        aria-valuemin="0"
        :aria-valuemax="booking.needing"
      >
        <span class="meter-fill" :style="{ width: `${booking.percent}%` }" />
      </div>
      <span class="tile-hint">
        <template v-if="booking.needing === 0">Nothing needs booking</template>
        <template v-else>
          {{ booking.booked }} booked · {{ booking.paid }} paid ·
          <span class="to-book">! {{ booking.toBook }} to book</span>
        </template>
      </span>
    </div>
  </section>
</template>

<style scoped>
.tiles {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) repeat(3, minmax(0, 1fr));
  gap: 1rem;
}

.tile {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 1.1rem 1.2rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-sm);
}

.tile.hero {
  color: #fff;
  background: linear-gradient(140deg, #2a6b5f 0%, #1a4f4a 100%);
  border-color: transparent;
}

.tile-label {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.hero .tile-label,
.hero .tile-hint {
  color: rgb(255 255 255 / 0.78);
}

.tile-value {
  font-family: var(--font-display);
  font-size: 1.75rem;
  font-weight: 700;
  line-height: 1.15;
  font-variant-numeric: tabular-nums;
}

.hero .tile-value {
  font-size: 2.1rem;
}

.tile-hint {
  font-size: 0.82rem;
  color: var(--color-muted);
}

.to-book {
  font-weight: 600;
  color: #8a5a00;
}

.meter {
  height: 0.45rem;
  margin: 0.2rem 0;
  overflow: hidden;
  background: rgb(250 178 25 / 0.35);
  border-radius: 999px;
}

.meter-fill {
  display: block;
  height: 100%;
  background: var(--status-good);
  border-radius: inherit;
}

@media (max-width: 900px) {
  .tiles {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .tile.hero {
    grid-column: 1 / -1;
  }
}
</style>

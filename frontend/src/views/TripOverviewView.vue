<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue'
import { RouterLink, routerKey } from 'vue-router'
import { fetchOverview } from '@/api/overview'
import CategorySpendChart from '@/components/CategorySpendChart.vue'
import SpendByDayChart from '@/components/SpendByDayChart.vue'
import TripPageHeader from '@/components/TripPageHeader.vue'
import type { TripOverview } from '@/types/overview'
import { formatDateRange, formatDayLabel } from '@/utils/date'
import { currencyName, formatCurrency, formatTwd } from '@/utils/money'
import { attentionView, share } from '@/utils/overview'
import { nightsLabel } from '@/utils/stays'

const props = defineProps<{
  id: string
}>()

const router = inject(routerKey, null)
const overview = ref<TripOverview | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

const tripLink = computed(() => ({ name: 'trip-schedule', params: { id: props.id } }))
const attention = computed(() =>
  overview.value
    ? overview.value.attention.map((item) => ({
        item,
        view: attentionView(item, overview.value!.startDate),
      }))
    : [],
)
const booking = computed(() => {
  const counts = overview.value?.booking ?? { toBook: 0, booked: 0, paid: 0 }
  const done = counts.booked + counts.paid
  const needing = done + counts.toBook
  return { ...counts, done, needing, percent: needing > 0 ? Math.round((done / needing) * 100) : 0 }
})
const activityShare = computed(() =>
  share(overview.value?.activitiesInTwd ?? null, overview.value?.totalInTwd ?? null),
)
const ratesSource = computed(() => {
  const updatedAt = overview.value?.ratesUpdatedAt

  if (!updatedAt) {
    return ''
  }

  const updated = new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(updatedAt))
  return `Totals in TWD · Rates by Exchange Rate API, updated ${updated}`
})
const hasSpend = computed(() => (overview.value?.totalInTwd ?? 0) > 0)

watch(
  () => props.id,
  async (id) => {
    loading.value = true
    error.value = null

    try {
      overview.value = await fetchOverview(id)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Something went wrong.'
    } finally {
      loading.value = false
    }
  },
  { immediate: true },
)

function dayLink(dayNumber: number) {
  return { ...tripLink.value, query: { view: 'timeline', day: String(dayNumber) } }
}

function attentionLink(kind: string, dayNumber: number) {
  return kind === 'ChecklistOpen'
    ? { name: 'trip-checklist', params: { id: props.id } }
    : dayLink(dayNumber)
}

function openDay(dayNumber: number) {
  router?.push(dayLink(dayNumber))
}

function twd(amount: number | null): string {
  return amount === null ? '—' : formatTwd(amount)
}
</script>

<template>
  <TripPageHeader
    v-if="overview"
    :name="overview.name"
    :meta="formatDateRange(overview.startDate, overview.endDate)"
  />
  <main class="page">
    <p v-if="loading" class="loading">Loading...</p>
    <div v-else-if="error" role="alert" class="alert" data-testid="overview-error">{{ error }}</div>

    <template v-else-if="overview">
      <header class="head">
        <span class="eyebrow">Where the money goes</span>
        <h1 class="display">Overview</h1>
        <p class="dates">
          {{ overview.days }} {{ overview.days === 1 ? 'day' : 'days' }} ·
          {{ overview.activityCount }}
          {{ overview.activityCount === 1 ? 'activity' : 'activities' }}
        </p>
      </header>

      <p v-if="!overview.ratesAvailable" class="rates-warning" data-testid="rates-warning">
        Exchange rates are unavailable, so totals in TWD cannot be shown right now.
      </p>

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

      <div class="grid">
        <section class="card panel wide" aria-labelledby="by-day-title">
          <div class="panel-head">
            <h2 id="by-day-title" class="panel-title">Spend by day</h2>
            <span class="panel-hint">Click a day to open it</span>
          </div>
          <SpendByDayChart :days="overview.spendByDay" @open-day="openDay" />
        </section>

        <section class="card panel" aria-labelledby="attention-title" data-testid="attention">
          <div class="panel-head">
            <h2 id="attention-title" class="panel-title">Needs attention</h2>
            <span class="chip">{{ attention.length }}</span>
          </div>
          <p v-if="attention.length === 0" class="all-set" data-testid="all-set">
            ✓ All set. Every night has a stay and nothing is waiting to be booked.
          </p>
          <ul v-else class="attention-list">
            <li
              v-for="entry in attention"
              :key="`${entry.item.kind}-${entry.item.date}-${entry.item.itemId}`"
            >
              <RouterLink
                :to="attentionLink(entry.item.kind, entry.view.dayNumber)"
                class="attention-item"
                :class="`kind-${entry.item.kind}`"
                data-testid="attention-item"
              >
                <span class="attention-icon" aria-hidden="true">{{ entry.view.icon }}</span>
                <span class="attention-text">
                  <span class="attention-title">{{ entry.view.title }}</span>
                  <span class="attention-detail">{{ entry.view.detail }}</span>
                </span>
                <span class="attention-go" aria-hidden="true">→</span>
              </RouterLink>
            </li>
          </ul>
        </section>

        <section class="card panel" aria-labelledby="category-title">
          <h2 id="category-title" class="panel-title">By category</h2>
          <CategorySpendChart :categories="overview.spendByCategory" :total="overview.totalInTwd" />
        </section>

        <section class="card panel" aria-labelledby="split-title" data-testid="spend-split">
          <h2 id="split-title" class="panel-title">Activities vs stays</h2>
          <template v-if="hasSpend">
            <div class="split" aria-hidden="true">
              <span
                v-if="overview.activitiesInTwd"
                class="split-part activities"
                :style="{ width: `${activityShare}%` }"
              />
              <span
                v-if="overview.staysInTwd"
                class="split-part stays"
                :style="{ width: `${100 - activityShare}%` }"
              />
            </div>
            <ul class="split-legend">
              <li>
                <span class="swatch activities" aria-hidden="true" />
                Activities
                <b data-testid="split-activities">{{ twd(overview.activitiesInTwd) }}</b>
                <small>{{ activityShare }}%</small>
              </li>
              <li>
                <span class="swatch stays" aria-hidden="true" />
                Stays
                <b data-testid="split-stays">{{ twd(overview.staysInTwd) }}</b>
                <small>{{ 100 - activityShare }}%</small>
              </li>
            </ul>
          </template>
          <p v-else class="muted">No costs yet.</p>

          <h3 class="sub-title">By currency</h3>
          <ul
            v-if="overview.spendByCurrency.length > 0"
            class="currency-list"
            data-testid="by-currency"
          >
            <li v-for="item in overview.spendByCurrency" :key="item.currency">
              <span class="currency-code" :title="currencyName(item.currency)">{{
                item.currency
              }}</span>
              <span class="currency-amount">{{ formatCurrency(item.amount, item.currency) }}</span>
              <span v-if="item.currency !== 'TWD'" class="currency-twd"
                >≈ {{ twd(item.amountInTwd) }}</span
              >
            </li>
          </ul>
          <p v-else class="muted">Nothing paid yet.</p>
        </section>

        <section class="card panel" aria-labelledby="top-title" data-testid="top-expenses">
          <h2 id="top-title" class="panel-title">Biggest expenses</h2>
          <ol v-if="overview.topExpenses.length > 0" class="expense-list">
            <li v-for="expense in overview.topExpenses" :key="expense.id" data-testid="top-expense">
              <span class="expense-icon" aria-hidden="true">{{
                expense.kind === 'Stay' ? '🛏' : '🎟️'
              }}</span>
              <span class="expense-text">
                <span class="expense-name">{{ expense.name }}</span>
                <span class="expense-date">{{ formatDayLabel(expense.date) }}</span>
              </span>
              <span class="expense-amount">
                {{ formatCurrency(expense.cost.amount, expense.cost.currency) }}
                <small v-if="expense.cost.currency !== 'TWD'">≈ {{ twd(expense.costInTwd) }}</small>
              </span>
            </li>
          </ol>
          <p v-else class="muted">No costs yet.</p>
        </section>
      </div>

      <p v-if="ratesSource" class="rates-source" data-testid="rates-source">{{ ratesSource }}</p>
    </template>
  </main>
</template>

<style scoped>
.page {
  --series-activities: #2a78d6;
  --series-stays: #eb6834;
  --status-good: #0ca30c;
  --status-warning: #fab219;
  max-width: 1120px;
  margin: 0 auto;
  padding: 2rem clamp(1rem, 3vw, 2.5rem) 4rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.loading,
.muted {
  color: var(--color-muted);
}

.muted {
  font-size: 0.88rem;
}

.head h1 {
  margin-top: 0.15rem;
  font-size: 2.2rem;
  line-height: 1.1;
}

.dates {
  margin-top: 0.3rem;
  color: var(--color-muted);
}

.rates-source {
  font-size: 0.78rem;
  text-align: right;
  color: var(--color-subtle);
}

.rates-warning {
  padding: 0.7rem 1rem;
  font-size: 0.88rem;
  color: #7a4a00;
  background: #fff4d6;
  border: 1px solid #f2d48a;
  border-radius: var(--radius-sm);
}

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

.grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1rem;
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  padding: 1.25rem 1.3rem;
}

.panel.wide {
  grid-column: span 2;
}

.panel-head {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.panel-title {
  font-family: var(--font-display);
  font-size: 1.15rem;
  font-weight: 700;
}

.panel-hint {
  margin-left: auto;
  font-size: 0.78rem;
  color: var(--color-subtle);
}

.sub-title {
  margin-top: 0.4rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.all-set {
  font-size: 0.9rem;
  color: #1f5c56;
}

.attention-list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  max-height: 19rem;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  list-style: none;
}

.attention-item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 0.7rem;
  color: var(--color-text);
  text-decoration: none;
  background: var(--color-surface-muted);
  border: 1px solid transparent;
  border-radius: 10px;
}

.attention-item:hover,
.attention-item:focus-visible {
  border-color: var(--color-primary);
  outline: none;
}

.attention-item.kind-ActivityToBook,
.attention-item.kind-StayToBook {
  background: #fdf1e6;
}

.attention-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.attention-title {
  overflow: hidden;
  font-size: 0.88rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.attention-detail {
  font-size: 0.76rem;
  color: var(--color-muted);
}

.attention-go {
  color: var(--color-subtle);
}

.split {
  display: flex;
  gap: 2px;
  height: 0.9rem;
}

.split-part {
  display: block;
  height: 100%;
}

.split-part:first-child {
  border-radius: 4px 0 0 4px;
}

.split-part:last-child {
  border-radius: 0 4px 4px 0;
}

.split-part:only-child {
  border-radius: 4px;
}

.split-part.activities,
.swatch.activities {
  background: var(--series-activities);
}

.split-part.stays,
.swatch.stays {
  background: var(--series-stays);
}

.split-legend,
.currency-list,
.expense-list {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.split-legend li {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  font-size: 0.88rem;
}

.split-legend b {
  margin-left: auto;
  font-variant-numeric: tabular-nums;
}

.split-legend small {
  width: 2.6rem;
  text-align: right;
  color: var(--color-muted);
}

.swatch {
  display: inline-block;
  width: 0.65rem;
  height: 0.65rem;
  border-radius: 3px;
}

.currency-list li {
  display: flex;
  align-items: baseline;
  gap: 0.6rem;
  font-size: 0.88rem;
}

.currency-code {
  width: 2.6rem;
  font-weight: 700;
  color: var(--color-muted);
}

.currency-amount {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.currency-twd {
  margin-left: auto;
  font-size: 0.8rem;
  color: var(--color-muted);
}

.expense-list li {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.expense-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.expense-name {
  overflow: hidden;
  font-size: 0.9rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.expense-date {
  font-size: 0.76rem;
  color: var(--color-muted);
}

.expense-amount {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  font-size: 0.88rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.expense-amount small {
  font-size: 0.74rem;
  font-weight: 400;
  color: var(--color-muted);
}

@media (max-width: 900px) {
  .tiles {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .tile.hero {
    grid-column: 1 / -1;
  }

  .grid {
    grid-template-columns: 1fr;
  }

  .panel.wide {
    grid-column: auto;
  }
}
</style>

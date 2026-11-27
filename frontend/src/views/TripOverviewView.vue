<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue'
import { errorMessage } from '@/utils/errors'
import { routerKey } from 'vue-router'
import { fetchOverview } from '@/api/overview'
import AttentionList from '@/components/AttentionList.vue'
import CategorySpendChart from '@/components/CategorySpendChart.vue'
import OverviewTiles from '@/components/OverviewTiles.vue'
import SpendByDayChart from '@/components/SpendByDayChart.vue'
import SpendSplit from '@/components/SpendSplit.vue'
import TopExpensesList from '@/components/TopExpensesList.vue'
import TripPageHeader from '@/components/TripPageHeader.vue'
import type { TripOverview } from '@/types/overview'
import { formatDateRange } from '@/utils/date'

const props = defineProps<{
  id: string
}>()

const router = inject(routerKey, null)
const overview = ref<TripOverview | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

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

watch(
  () => props.id,
  async (id) => {
    loading.value = true
    error.value = null

    try {
      overview.value = await fetchOverview(id)
    } catch (e) {
      error.value = errorMessage(e)
    } finally {
      loading.value = false
    }
  },
  { immediate: true },
)

function openDay(dayNumber: number) {
  router?.push({
    name: 'trip-schedule',
    params: { id: props.id },
    query: { view: 'timeline', day: String(dayNumber) },
  })
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

      <OverviewTiles :overview="overview" />

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
            <span class="chip">{{ overview.attention.length }}</span>
          </div>
          <AttentionList
            :trip-id="id"
            :items="overview.attention"
            :start-date="overview.startDate"
          />
        </section>

        <section class="card panel" aria-labelledby="category-title">
          <h2 id="category-title" class="panel-title">By category</h2>
          <CategorySpendChart :categories="overview.spendByCategory" :total="overview.totalInTwd" />
        </section>

        <section class="card panel" aria-labelledby="split-title" data-testid="spend-split">
          <h2 id="split-title" class="panel-title">Activities vs stays</h2>
          <SpendSplit :overview="overview" />
        </section>

        <section class="card panel" aria-labelledby="top-title" data-testid="top-expenses">
          <h2 id="top-title" class="panel-title">Biggest expenses</h2>
          <TopExpensesList :expenses="overview.topExpenses" />
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

.loading {
  color: var(--color-muted);
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

@media (max-width: 900px) {
  .grid {
    grid-template-columns: 1fr;
  }

  .panel.wide {
    grid-column: auto;
  }
}
</style>

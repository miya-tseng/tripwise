<script setup lang="ts">
import { computed } from 'vue'
import type { TripOverview } from '@/types/overview'
import { currencyName, formatCurrency, formatTwd } from '@/utils/money'
import { share } from '@/utils/overview'

const props = defineProps<{
  overview: TripOverview
}>()

const activityShare = computed(() =>
  share(props.overview.activitiesInTwd, props.overview.totalInTwd),
)
const hasSpend = computed(() => (props.overview.totalInTwd ?? 0) > 0)

function twd(amount: number | null): string {
  return amount === null ? '—' : formatTwd(amount)
}
</script>

<template>
  <div class="panel-content">
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
    <ul v-if="overview.spendByCurrency.length > 0" class="currency-list" data-testid="by-currency">
      <li v-for="item in overview.spendByCurrency" :key="item.currency">
        <span class="currency-code" :title="currencyName(item.currency)">{{ item.currency }}</span>
        <span class="currency-amount">{{ formatCurrency(item.amount, item.currency) }}</span>
        <span v-if="item.currency !== 'TWD'" class="currency-twd"
          >≈ {{ twd(item.amountInTwd) }}</span
        >
      </li>
    </ul>
    <p v-else class="muted">Nothing paid yet.</p>
  </div>
</template>

<style scoped>
.panel-content {
  display: contents;
}

.muted {
  font-size: 0.88rem;
  color: var(--color-muted);
}

.split-legend,
.currency-list {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.sub-title {
  margin-top: 0.4rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-muted);
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
</style>

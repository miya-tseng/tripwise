<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DaySpend } from '@/types/overview'
import { dateParts, formatDayLabel } from '@/utils/date'
import { formatTwd } from '@/utils/money'
import { axisTicks, compactTwd, dayMax, percentOf } from '@/utils/overview'

const props = defineProps<{
  days: DaySpend[]
}>()

const emit = defineEmits<{
  openDay: [dayNumber: number]
}>()

const hovered = ref<DaySpend | null>(null)

const max = computed(() => dayMax(props.days))
const ticks = computed(() => axisTicks(max.value).reverse())
const hasSpend = computed(() => max.value > 0)

function amountText(amount: number | null): string {
  return amount === null ? '—' : formatTwd(amount)
}

function label(day: DaySpend): string {
  return `Day ${day.dayNumber}, ${formatDayLabel(day.date)}: ${amountText(day.totalInTwd)}`
}
</script>

<template>
  <div class="chart" data-testid="spend-by-day">
    <ul class="legend" aria-label="Legend">
      <li><span class="swatch activities" aria-hidden="true" />Activities</li>
      <li><span class="swatch stays" aria-hidden="true" />Stays</li>
    </ul>

    <p v-if="!hasSpend" class="empty" data-testid="spend-by-day-empty">
      No costs yet. Add a cost to an activity or a stay to see it here.
    </p>

    <div v-else class="plot">
      <div class="y-axis" aria-hidden="true">
        <span v-for="tick in ticks" :key="tick">{{ compactTwd(tick) }}</span>
      </div>
      <div class="area">
        <div class="gridlines" aria-hidden="true">
          <span v-for="tick in ticks" :key="tick" />
        </div>
        <div class="columns">
          <button
            v-for="day in days"
            :key="day.date"
            type="button"
            class="column"
            :class="{ active: hovered?.date === day.date }"
            :aria-label="label(day)"
            data-testid="day-bar"
            @mouseenter="hovered = day"
            @mouseleave="hovered = null"
            @focus="hovered = day"
            @blur="hovered = null"
            @click="emit('openDay', day.dayNumber)"
          >
            <span class="stack">
              <span
                v-if="day.stayInTwd"
                class="segment stays"
                :class="{ top: true }"
                :style="{ height: `${percentOf(day.stayInTwd, max)}%` }"
                data-testid="day-bar-stay"
              />
              <span
                v-if="day.activitiesInTwd"
                class="segment activities"
                :class="{ top: !day.stayInTwd }"
                :style="{ height: `${percentOf(day.activitiesInTwd, max)}%` }"
                data-testid="day-bar-activities"
              />
            </span>
            <span
              v-if="hovered?.date === day.date"
              class="tooltip"
              role="tooltip"
              data-testid="day-tooltip"
            >
              <strong>Day {{ day.dayNumber }} · {{ formatDayLabel(day.date) }}</strong>
              <span class="tooltip-row">
                <span class="swatch activities" aria-hidden="true" />Activities
                <b>{{ amountText(day.activitiesInTwd) }}</b>
              </span>
              <span class="tooltip-row">
                <span class="swatch stays" aria-hidden="true" />Stay
                <b>{{ amountText(day.stayInTwd) }}</b>
              </span>
              <span class="tooltip-row total">
                Total <b>{{ amountText(day.totalInTwd) }}</b>
              </span>
            </span>
          </button>
        </div>
        <div class="x-axis" aria-hidden="true">
          <span v-for="day in days" :key="day.date">
            <b>D{{ day.dayNumber }}</b>
            <small>{{ dateParts(day.date).month }} {{ dateParts(day.date).day }}</small>
          </span>
        </div>
      </div>
    </div>

    <table class="visually-hidden">
      <caption>
        Spend by day in TWD
      </caption>
      <thead>
        <tr>
          <th scope="col">Day</th>
          <th scope="col">Activities</th>
          <th scope="col">Stay</th>
          <th scope="col">Total</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="day in days" :key="day.date">
          <th scope="row">Day {{ day.dayNumber }}</th>
          <td>{{ amountText(day.activitiesInTwd) }}</td>
          <td>{{ amountText(day.stayInTwd) }}</td>
          <td>{{ amountText(day.totalInTwd) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.chart {
  --series-activities: #2a78d6;
  --series-stays: #eb6834;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.legend {
  display: flex;
  gap: 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
  font-size: 0.8rem;
  color: var(--color-muted);
}

.legend li {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.swatch {
  display: inline-block;
  width: 0.65rem;
  height: 0.65rem;
  border-radius: 3px;
}

.swatch.activities {
  background: var(--series-activities);
}

.swatch.stays {
  background: var(--series-stays);
}

.empty {
  padding: 2.5rem 1rem;
  font-size: 0.9rem;
  text-align: center;
  color: var(--color-muted);
}

.plot {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.6rem;
}

.y-axis {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 13rem;
  font-size: 0.7rem;
  text-align: right;
  color: var(--color-subtle);
  font-variant-numeric: tabular-nums;
  transform: translateY(-0.4em);
}

.area {
  position: relative;
  min-width: 0;
}

.gridlines {
  position: absolute;
  inset: 0 0 auto;
  height: 13rem;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  pointer-events: none;
}

.gridlines span {
  border-top: 1px solid var(--color-border);
}

.gridlines span:last-child {
  border-top-color: var(--color-border-strong);
}

.columns {
  position: relative;
  display: flex;
  align-items: stretch;
  gap: 0.35rem;
  height: 13rem;
}

.column {
  position: relative;
  flex: 1;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  min-width: 0;
  padding: 0;
  background: none;
  border: 0;
  border-radius: 6px;
  cursor: pointer;
}

.column.active,
.column:focus-visible {
  background: rgb(27 36 49 / 0.05);
  outline: none;
}

.stack {
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 2px;
  width: min(70%, 2.6rem);
  height: 100%;
}

.segment {
  display: block;
  min-height: 2px;
}

.segment.top {
  border-radius: 4px 4px 0 0;
}

.segment.activities {
  background: var(--series-activities);
}

.segment.stays {
  background: var(--series-stays);
}

.tooltip {
  position: absolute;
  bottom: calc(100% - 1.5rem);
  left: 50%;
  z-index: 5;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 11rem;
  padding: 0.6rem 0.75rem;
  font-size: 0.78rem;
  text-align: left;
  white-space: nowrap;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 10px;
  box-shadow: var(--shadow-lg);
  transform: translateX(-50%);
  pointer-events: none;
}

.column:first-child .tooltip {
  left: 0;
  transform: none;
}

.column:last-child .tooltip {
  right: 0;
  left: auto;
  transform: none;
}

.tooltip-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  color: var(--color-muted);
}

.tooltip-row b {
  margin-left: auto;
  padding-left: 1rem;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}

.tooltip-row.total {
  padding-top: 0.25rem;
  border-top: 1px solid var(--color-border);
}

.x-axis {
  display: flex;
  gap: 0.35rem;
  margin-top: 0.45rem;
}

.x-axis span {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 0;
  font-size: 0.7rem;
  line-height: 1.2;
  color: var(--color-muted);
}

.x-axis b {
  color: var(--color-text);
}

.x-axis small {
  font-size: 0.65rem;
  white-space: nowrap;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}
</style>

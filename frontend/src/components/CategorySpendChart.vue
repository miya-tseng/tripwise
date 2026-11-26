<script setup lang="ts">
import { computed, ref } from 'vue'
import type { CategorySpend } from '@/types/overview'
import { categoryMeta } from '@/utils/activityDetails'
import { formatTwd } from '@/utils/money'
import { percentOf, share } from '@/utils/overview'

const props = defineProps<{
  categories: CategorySpend[]
  total: number | null
}>()

const hovered = ref<string | null>(null)

const max = computed(() => Math.max(0, ...props.categories.map((item) => item.amountInTwd ?? 0)))
const withSpend = computed(() => props.categories.filter((item) => (item.amountInTwd ?? 0) > 0))
const withoutSpend = computed(() => props.categories.filter((item) => !item.amountInTwd))

function activitiesLabel(count: number): string {
  return count === 1 ? '1 activity' : `${count} activities`
}
</script>

<template>
  <div class="chart" data-testid="spend-by-category">
    <p v-if="withSpend.length === 0" class="empty" data-testid="spend-by-category-empty">
      No activity costs yet.
    </p>
    <ul v-else class="bars">
      <li
        v-for="item in withSpend"
        :key="item.category"
        class="bar-row"
        :class="{ active: hovered === item.category }"
        tabindex="0"
        :aria-label="`${categoryMeta(item.category).label}: ${formatTwd(item.amountInTwd ?? 0)}, ${activitiesLabel(item.activityCount)}`"
        data-testid="category-bar"
        @mouseenter="hovered = item.category"
        @mouseleave="hovered = null"
        @focus="hovered = item.category"
        @blur="hovered = null"
      >
        <span class="bar-label">
          <span aria-hidden="true">{{ categoryMeta(item.category).icon }}</span>
          {{ categoryMeta(item.category).label }}
        </span>
        <span class="track">
          <span class="fill" :style="{ width: `${percentOf(item.amountInTwd, max)}%` }" />
        </span>
        <span class="bar-value" data-testid="category-value">
          {{ formatTwd(item.amountInTwd ?? 0) }}
        </span>
        <span
          v-if="hovered === item.category"
          class="tooltip"
          role="tooltip"
          data-testid="category-tooltip"
        >
          {{ activitiesLabel(item.activityCount) }} · {{ share(item.amountInTwd, total) }}% of the
          trip
        </span>
      </li>
    </ul>
    <p v-if="withoutSpend.length > 0" class="free" data-testid="free-categories">
      No cost:
      {{ withoutSpend.map((item) => categoryMeta(item.category).label).join(', ') }}
    </p>
  </div>
</template>

<style scoped>
.chart {
  --series-fill: #2a78d6;
}

.empty,
.free {
  font-size: 0.85rem;
  color: var(--color-muted);
}

.empty {
  padding: 1.5rem 0;
  text-align: center;
}

.free {
  margin-top: 0.75rem;
}

.bars {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.bar-row {
  position: relative;
  display: grid;
  grid-template-columns: 7.5rem 1fr auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.35rem 0.4rem;
  border-radius: 8px;
  outline: none;
}

.bar-row.active {
  background: rgb(27 36 49 / 0.05);
}

.bar-label {
  overflow: hidden;
  font-size: 0.85rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.track {
  display: block;
  height: 0.75rem;
}

.fill {
  display: block;
  height: 100%;
  min-width: 2px;
  background: var(--series-fill);
  border-radius: 0 4px 4px 0;
}

.bar-value {
  font-size: 0.85rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.tooltip {
  position: absolute;
  right: 0;
  bottom: calc(100% + 0.2rem);
  z-index: 5;
  padding: 0.4rem 0.65rem;
  font-size: 0.75rem;
  white-space: nowrap;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  box-shadow: var(--shadow-lg);
  pointer-events: none;
}
</style>

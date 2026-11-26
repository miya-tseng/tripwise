<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Activity, ScheduleDay } from '@/types/activity'
import {
  DEFAULT_SORT,
  nextSort,
  tableGroups,
  type SortKey,
  type TableSort,
} from '@/utils/activityTable'
import { bookingStatusMeta, categoryMeta } from '@/utils/activityDetails'
import { formatDayLabel } from '@/utils/date'
import { formatCurrency, formatTwd } from '@/utils/money'
import { formatTime } from '@/utils/schedule'

const props = defineProps<{
  days: ScheduleDay[]
}>()

const emit = defineEmits<{
  edit: [activity: Activity]
}>()

const sort = ref<TableSort>(DEFAULT_SORT)

const groups = computed(() => tableGroups(props.days, sort.value))
const grouped = computed(() => sort.value.key === 'date')

const columns: { key: SortKey | null; label: string }[] = [
  { key: 'date', label: 'Date' },
  { key: 'name', label: 'Name' },
  { key: 'category', label: 'Category' },
  { key: 'booking', label: 'Booking' },
  { key: 'cost', label: 'Cost' },
  { key: null, label: 'Place' },
]

function ariaSort(key: SortKey | null): 'ascending' | 'descending' | undefined {
  if (key === null || sort.value.key !== key) {
    return undefined
  }

  return sort.value.direction === 'asc' ? 'ascending' : 'descending'
}

function sortBy(key: SortKey) {
  sort.value = nextSort(sort.value, key)
}

function subtotalText(total: number | null): string {
  return total === null ? '—' : formatTwd(total)
}

function isForeign(activity: Activity): boolean {
  return activity.cost > 0 && activity.currency !== 'TWD'
}
</script>

<template>
  <div class="table-wrap card" data-testid="table-view">
    <p v-if="groups.length === 0" class="empty" data-testid="table-empty">No activities to show.</p>
    <table v-else class="activity-table">
      <thead>
        <tr>
          <th
            v-for="column in columns"
            :key="column.label"
            scope="col"
            :aria-sort="ariaSort(column.key)"
            :class="{ numeric: column.key === 'cost' }"
          >
            <button
              v-if="column.key"
              type="button"
              class="sort-button"
              :data-testid="`sort-${column.key}`"
              @click="sortBy(column.key)"
            >
              {{ column.label }}
              <span class="sort-arrow" aria-hidden="true">{{
                sort.key === column.key ? (sort.direction === 'asc' ? '▲' : '▼') : ''
              }}</span>
            </button>
            <span v-else>{{ column.label }}</span>
          </th>
        </tr>
      </thead>
      <tbody v-for="group in groups" :key="group.date ?? 'all'">
        <tr v-if="grouped && group.date" class="group-row" data-testid="table-day">
          <th scope="rowgroup" colspan="4">
            Day {{ group.dayNumber }} · {{ formatDayLabel(group.date) }}
          </th>
          <td class="numeric subtotal" data-testid="table-day-total">
            {{ subtotalText(group.totalInTwd) }}
          </td>
          <td />
        </tr>
        <tr
          v-for="row in group.rows"
          :key="row.activity.id"
          class="activity-row"
          tabindex="0"
          data-testid="table-row"
          @click="emit('edit', row.activity)"
          @keydown.enter="emit('edit', row.activity)"
        >
          <td class="when">
            <span v-if="!grouped" class="row-day">Day {{ row.dayNumber }}</span>
            {{ formatTime(row.activity.startTime) }}–{{ formatTime(row.activity.endTime) }}
          </td>
          <td class="name" data-testid="table-name">{{ row.activity.name }}</td>
          <td>
            <span class="category">
              {{ categoryMeta(row.activity.category).icon }}
              {{ categoryMeta(row.activity.category).label }}
            </span>
          </td>
          <td>
            <span
              v-if="row.activity.bookingStatus !== 'NotNeeded'"
              class="booking-badge"
              :class="`tone-${bookingStatusMeta(row.activity.bookingStatus).tone}`"
            >
              {{ bookingStatusMeta(row.activity.bookingStatus).label }}
            </span>
          </td>
          <td class="numeric" data-testid="table-cost">
            <template v-if="row.activity.cost > 0">
              <span>{{ formatCurrency(row.activity.cost, row.activity.currency) }}</span>
              <span
                v-if="isForeign(row.activity) && row.activity.costInTwd !== null"
                class="converted"
              >
                ≈ {{ formatTwd(row.activity.costInTwd) }}
              </span>
            </template>
          </td>
          <td class="place">{{ row.activity.place?.name ?? '' }}</td>
        </tr>
        <tr v-if="!grouped" class="group-row total-row">
          <th scope="row" colspan="4">Total</th>
          <td class="numeric subtotal" data-testid="table-total">
            {{ subtotalText(group.totalInTwd) }}
          </td>
          <td />
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
  padding: 0;
}

.empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-muted);
}

.activity-table {
  width: 100%;
  min-width: 760px;
  border-collapse: collapse;
  font-size: 0.9rem;
}

th,
td {
  padding: 0.6rem 0.9rem;
  text-align: left;
  vertical-align: middle;
  border-bottom: 1px solid var(--color-border);
}

thead th {
  position: sticky;
  top: 0;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-muted);
  background: var(--color-surface);
}

.sort-button {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0;
  font: inherit;
  letter-spacing: inherit;
  text-transform: inherit;
  color: inherit;
  background: none;
  border: 0;
  cursor: pointer;
}

.sort-button:hover,
th[aria-sort] .sort-button {
  color: var(--color-text);
}

.sort-arrow {
  min-width: 0.7rem;
  font-size: 0.6rem;
}

.numeric {
  text-align: right;
}

th.numeric .sort-button {
  flex-direction: row-reverse;
}

.group-row th,
.group-row td {
  padding-block: 0.5rem;
  font-weight: 700;
  color: var(--color-primary);
  background: var(--color-primary-soft);
}

.total-row th,
.total-row td {
  color: var(--color-text);
  background: var(--color-surface-muted);
}

.activity-row {
  cursor: pointer;
  transition: background 0.12s;
}

.activity-row:hover,
.activity-row:focus-visible {
  background: rgb(31 92 86 / 0.05);
  outline: none;
}

.when {
  white-space: nowrap;
  color: var(--color-muted);
  font-variant-numeric: tabular-nums;
}

.row-day {
  margin-right: 0.35rem;
  font-weight: 700;
  color: var(--color-text);
}

.name {
  font-weight: 600;
}

.category {
  white-space: nowrap;
}

.booking-badge {
  display: inline-flex;
  padding: 0.05rem 0.5rem;
  font-size: 0.72rem;
  font-weight: 700;
  white-space: nowrap;
  border-radius: 999px;
  color: var(--color-muted);
  background: var(--color-surface-muted);
}

.booking-badge.tone-warning {
  color: #9a4a1c;
  background: #fde8d7;
}

.booking-badge.tone-success {
  color: #1f5c56;
  background: #e1efe9;
}

td.numeric {
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

.converted {
  display: block;
  font-size: 0.78rem;
  color: var(--color-muted);
}

.place {
  color: var(--color-muted);
}
</style>

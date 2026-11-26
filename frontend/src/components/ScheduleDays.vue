<script setup lang="ts">
import { computed, ref } from 'vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { Activity, ScheduleDay } from '@/types/activity'
import { dateParts, formatDayLabel } from '@/utils/date'
import { HOME_CURRENCY } from '@/types/currency'
import {
  dayTotalInTwd,
  formatCurrency,
  formatTotals,
  formatTwd,
  totalsByCurrency,
} from '@/utils/money'
import { formatTime } from '@/utils/schedule'

withDefaults(
  defineProps<{
    days: ScheduleDay[]
    editingId?: string | null
    deletingId?: string | null
  }>(),
  {
    editingId: null,
    deletingId: null,
  },
)

const emit = defineEmits<{
  edit: [activity: Activity]
  delete: [id: string]
}>()

const activityToDelete = ref<Activity | null>(null)

function isForeign(activity: Activity): boolean {
  return activity.cost > 0 && activity.currency !== HOME_CURRENCY
}

function dayTotalText(day: ScheduleDay): string {
  const total = dayTotalInTwd(day)
  return total === null ? formatTotals(totalsByCurrency(day.activities)) : formatTwd(total)
}

const confirmMessage = computed(() =>
  activityToDelete.value ? `Delete "${activityToDelete.value.name}" from your schedule?` : '',
)

function confirmDelete() {
  if (activityToDelete.value) {
    emit('delete', activityToDelete.value.id)
  }
  activityToDelete.value = null
}
</script>

<template>
  <ol class="days">
    <li v-for="(day, index) in days" :key="day.date" class="card day" data-testid="schedule-day">
      <header class="day-header">
        <div class="date-tile" aria-hidden="true">
          <span class="tile-month">{{ dateParts(day.date).month }}</span>
          <span class="tile-day">{{ dateParts(day.date).day }}</span>
        </div>
        <div class="day-heading">
          <span class="eyebrow">Day {{ index + 1 }}</span>
          <h3 class="day-date" data-testid="day-date">{{ formatDayLabel(day.date) }}</h3>
        </div>
        <div class="day-summary">
          <span class="day-total" data-testid="day-total">{{ dayTotalText(day) }}</span>
          <span class="day-count">
            {{ day.activities.length }}
            {{ day.activities.length === 1 ? 'activity' : 'activities' }}
          </span>
        </div>
      </header>

      <p v-if="day.activities.length === 0" class="day-empty" data-testid="day-empty">
        Nothing planned yet.
      </p>
      <ul v-else class="timeline">
        <li
          v-for="activity in day.activities"
          :key="activity.id"
          class="activity"
          :class="{ editing: activity.id === editingId }"
          data-testid="activity-item"
        >
          <span class="dot" aria-hidden="true" />
          <span class="activity-time" data-testid="activity-time">
            {{ formatTime(activity.startTime) }} – {{ formatTime(activity.endTime) }}
          </span>
          <span class="activity-name" data-testid="activity-name">{{ activity.name }}</span>
          <span class="activity-cost" :class="{ free: activity.cost === 0 }">
            <span data-testid="activity-cost">
              {{ activity.cost === 0 ? 'Free' : formatCurrency(activity.cost, activity.currency) }}
            </span>
            <span
              v-if="isForeign(activity) && activity.costInTwd !== null"
              class="activity-cost-twd"
              data-testid="activity-cost-twd"
            >
              ≈ {{ formatTwd(activity.costInTwd) }}
            </span>
          </span>
          <div class="activity-actions">
            <button
              type="button"
              class="btn btn-ghost btn-small"
              data-testid="edit-activity"
              @click="emit('edit', activity)"
            >
              Edit
            </button>
            <button
              type="button"
              class="btn btn-ghost btn-small delete"
              data-testid="delete-activity"
              :disabled="deletingId === activity.id"
              @click="activityToDelete = activity"
            >
              {{ deletingId === activity.id ? 'Deleting...' : 'Delete' }}
            </button>
          </div>
        </li>
      </ul>
    </li>
  </ol>

  <ConfirmDialog
    :open="activityToDelete !== null"
    title="Delete activity?"
    :message="confirmMessage"
    confirm-label="Delete"
    @confirm="confirmDelete"
    @cancel="activityToDelete = null"
  />
</template>

<style scoped>
.days {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.day {
  padding: 1.25rem 1.4rem 1.35rem;
}

.day-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.date-tile {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 3.4rem;
  height: 3.6rem;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 12px;
  line-height: 1;
}

.tile-month {
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.tile-day {
  margin-top: 0.2rem;
  font-family: var(--font-display);
  font-size: 1.55rem;
  font-weight: 700;
}

.day-heading {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}

.day-date {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.day-summary {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.05rem;
  text-align: right;
}

.day-total {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.day-count {
  font-size: 0.78rem;
  color: var(--color-subtle);
}

.day-empty {
  margin: 1rem 0 0 4.4rem;
  padding: 0.85rem 1rem;
  font-size: 0.9rem;
  color: var(--color-subtle);
  border: 1.5px dashed var(--color-border);
  border-radius: var(--radius-sm);
}

.timeline {
  position: relative;
  list-style: none;
  margin: 1rem 0 0;
  padding: 0 0 0 4.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.timeline::before {
  content: '';
  position: absolute;
  top: 1.1rem;
  bottom: 1.1rem;
  left: 1.7rem;
  width: 2px;
  background: linear-gradient(var(--color-primary-soft), var(--color-border));
  border-radius: 2px;
}

.activity {
  position: relative;
  display: flex;
  align-items: center;
  gap: 1.1rem;
  min-height: 3rem;
  padding: 0.45rem 0.5rem 0.45rem 1rem;
  border: 1px solid transparent;
  border-radius: 12px;
  transition:
    background-color 0.15s,
    border-color 0.15s;
}

.activity:hover {
  background: var(--color-surface-muted);
  border-color: var(--color-border);
}

.activity.editing {
  background: var(--color-primary-soft);
  border-color: rgb(31 92 86 / 0.25);
}

.dot {
  position: absolute;
  left: -2.95rem;
  top: 50%;
  width: 0.75rem;
  height: 0.75rem;
  margin-top: -0.375rem;
  background: var(--color-surface);
  border: 2.5px solid var(--color-primary);
  border-radius: 50%;
  box-shadow: 0 0 0 4px var(--color-surface);
}

.activity.editing .dot {
  background: var(--color-primary);
}

.activity-time {
  flex-shrink: 0;
  min-width: 7.2rem;
  font-size: 0.9rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-muted);
}

.activity-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-cost {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  min-width: 5.5rem;
  font-size: 0.92rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  text-align: right;
  color: var(--color-accent);
}

.activity-cost-twd {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-subtle);
}

.activity-cost.free {
  font-weight: 500;
  color: var(--color-subtle);
}

.activity-actions {
  display: flex;
  gap: 0.15rem;
  opacity: 0.55;
  transition: opacity 0.15s;
}

.activity:hover .activity-actions,
.activity:focus-within .activity-actions,
.activity.editing .activity-actions {
  opacity: 1;
}

.delete:hover:not(:disabled) {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}

@media (hover: none) {
  .activity-actions {
    opacity: 1;
  }
}

@media (max-width: 560px) {
  .day {
    padding: 1.1rem 1rem 1.2rem;
  }

  .timeline {
    padding-left: 1.6rem;
  }

  .timeline::before {
    left: 0.45rem;
  }

  .dot {
    left: -1.5rem;
  }

  .day-empty {
    margin-left: 0;
  }

  .activity {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    grid-template-areas:
      'time cost actions'
      'name cost actions';
    gap: 0.1rem 0.5rem;
    padding-left: 0.6rem;
  }

  .activity-time {
    grid-area: time;
    min-width: 0;
    font-size: 0.82rem;
  }

  .activity-name {
    grid-area: name;
    white-space: normal;
  }

  .activity-cost {
    grid-area: cost;
    min-width: 0;
  }

  .activity-actions {
    grid-area: actions;
  }
}
</style>

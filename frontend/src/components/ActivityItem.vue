<script setup lang="ts">
import { computed } from 'vue'
import type { Activity } from '@/types/activity'
import { HOME_CURRENCY } from '@/types/currency'
import { bookingStatusMeta, categoryMeta, notesPreview } from '@/utils/activityDetails'
import { placeLink } from '@/utils/mapsLinks'
import { formatCurrency, formatTwd } from '@/utils/money'
import { formatTime } from '@/utils/schedule'

const props = withDefaults(
  defineProps<{
    activity: Activity
    editing?: boolean
    highlighted?: boolean
    deleting?: boolean
  }>(),
  {
    editing: false,
    highlighted: false,
    deleting: false,
  },
)

const emit = defineEmits<{
  edit: [activity: Activity]
  delete: [activity: Activity]
  suggest: [activity: Activity]
}>()

const isForeign = computed(
  () => props.activity.cost > 0 && props.activity.currency !== HOME_CURRENCY,
)

function onRowClick(event: MouseEvent) {
  const target = event.target as HTMLElement

  if (!target.closest('a, button, input, label')) {
    emit('edit', props.activity)
  }
}
</script>

<template>
  <li
    class="activity"
    :class="[`category-${activity.category.toLowerCase()}`, { editing, highlighted }]"
    :data-activity-id="activity.id"
    data-testid="activity-item"
    @click="onRowClick"
  >
    <span class="dot" aria-hidden="true" />
    <span class="activity-time" data-testid="activity-time">
      {{ formatTime(activity.startTime) }} – {{ formatTime(activity.endTime) }}
    </span>
    <span class="activity-main">
      <span class="activity-title">
        <span class="activity-heading">
          <span
            class="category-icon"
            :title="categoryMeta(activity.category).label"
            data-testid="activity-category"
            >{{ categoryMeta(activity.category).icon }}</span
          >
          <span class="activity-name" data-testid="activity-name">{{ activity.name }}</span>
        </span>
        <span
          v-if="activity.bookingStatus !== 'NotNeeded'"
          class="booking-badge"
          :class="`tone-${bookingStatusMeta(activity.bookingStatus).tone}`"
          data-testid="booking-badge"
        >
          {{ bookingStatusMeta(activity.bookingStatus).label }}
        </span>
      </span>
      <span v-if="activity.place" class="activity-place">
        <a
          v-if="activity.place"
          class="map-link"
          :href="placeLink(activity.place)"
          target="_blank"
          rel="noopener noreferrer"
          :aria-label="`Open ${activity.name} in Google Maps`"
          data-testid="activity-map-link"
        >
          <svg viewBox="0 0 20 20" aria-hidden="true">
            <path
              d="M10 18s-5.5-5.2-5.5-9.3a5.5 5.5 0 1 1 11 0C15.5 12.8 10 18 10 18Z"
              fill="none"
              stroke="currentColor"
              stroke-width="1.6"
            />
            <circle cx="10" cy="8.6" r="1.9" fill="currentColor" />
          </svg>
        </a>
        <span class="activity-address" data-testid="activity-address">
          {{ activity.place.address }}
        </span>
      </span>
      <span v-if="activity.notes" class="activity-notes" data-testid="activity-notes">
        {{ notesPreview(activity.notes) }}
      </span>
      <a
        v-if="activity.link"
        class="activity-link"
        :href="activity.link"
        target="_blank"
        rel="noopener noreferrer"
        data-testid="activity-link"
      >
        {{ activity.confirmationCode ? `Booking ${activity.confirmationCode}` : 'Open link' }}
        ↗
      </a>
      <span
        v-else-if="activity.confirmationCode"
        class="activity-confirmation"
        data-testid="activity-confirmation"
      >
        Booking {{ activity.confirmationCode }}
      </span>
    </span>
    <span class="activity-cost" :class="{ free: activity.cost === 0 }">
      <span data-testid="activity-cost">
        {{ activity.cost === 0 ? 'Free' : formatCurrency(activity.cost, activity.currency) }}
      </span>
      <span
        v-if="isForeign && activity.costInTwd !== null"
        class="activity-cost-twd"
        data-testid="activity-cost-twd"
      >
        ≈ {{ formatTwd(activity.costInTwd) }}
      </span>
    </span>
    <div class="activity-actions">
      <button
        v-if="activity.place"
        type="button"
        class="btn btn-ghost btn-small ideas"
        data-testid="suggest-activity"
        @click="emit('suggest', activity)"
        :aria-label="`Ideas near ${activity.name}`"
      >
        <span class="action-icon" aria-hidden="true">💡</span>
        <span class="action-label">Ideas</span>
      </button>
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
        :disabled="deleting"
        @click="emit('delete', activity)"
      >
        {{ deleting ? 'Deleting...' : 'Delete' }}
      </button>
    </div>
  </li>
</template>

<style scoped>
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
  border: 2.5px solid var(--category-color, var(--color-primary));
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

.activity-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.activity-title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.15rem 0.4rem;
  min-width: 0;
}

.category-icon {
  flex-shrink: 0;
  font-size: 0.95rem;
}

.activity-title .activity-name {
  min-width: 0;
  white-space: normal;
  overflow-wrap: anywhere;
}

.activity-notes {
  overflow: hidden;
  font-size: 0.78rem;
  color: var(--color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-link,
.activity-confirmation {
  align-self: flex-start;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
}

.activity-link:hover {
  text-decoration: underline;
}

.activity-address {
  overflow: hidden;
  font-size: 0.78rem;
  color: var(--color-subtle);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-name {
  min-width: 0;
  overflow: hidden;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-heading {
  min-width: 0;
}

.activity-heading .category-icon {
  margin-right: 0.3rem;
}

.activity-place {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  min-width: 0;
}

.map-link {
  flex-shrink: 0;
  display: grid;
  place-items: center;
  width: 1.5rem;
  height: 1.5rem;
  color: var(--color-accent);
  border-radius: 8px;
  transition: background-color 0.15s;
}

.map-link svg {
  width: 0.95rem;
  height: 0.95rem;
}

.map-link:hover,
.map-link:focus-visible {
  outline: none;
  background: var(--color-accent-soft);
}

.activity.highlighted {
  background: var(--color-accent-soft);
  border-color: rgb(201 113 75 / 0.35);
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

.ideas {
  color: var(--color-accent);
}

.ideas:hover:not(:disabled) {
  color: var(--color-accent);
  background: var(--color-accent-soft);
}

.delete:hover:not(:disabled) {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}

.activity {
  cursor: pointer;
}

.action-icon {
  display: none;
}

@media (hover: none) {
  .activity-actions {
    opacity: 1;
  }
}

@media (max-width: 560px) {
  .dot {
    left: -1.35rem;
  }

  .activity {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    grid-template-areas:
      'time cost'
      'name cost'
      'actions actions';
    gap: 0.15rem 0.6rem;
    padding: 0.55rem 0.4rem 0.4rem 0.6rem;
  }

  .activity-time {
    grid-area: time;
    min-width: 0;
    font-size: 0.8rem;
  }

  .activity-main {
    grid-area: name;
  }

  .activity-cost {
    grid-area: cost;
    align-self: start;
    min-width: 0;
  }

  .activity {
    grid-template-areas:
      'time cost'
      'name actions';
  }

  .activity-actions {
    grid-area: actions;
    align-self: end;
    justify-content: flex-end;
    opacity: 1;
  }

  .activity-actions [data-testid='edit-activity'],
  .activity-actions [data-testid='delete-activity'],
  .action-label {
    display: none;
  }

  .action-icon {
    display: inline;
  }

  .activity-actions .ideas {
    width: 2.25rem;
    padding: 0;
  }

  .activity-notes {
    display: -webkit-box;
    overflow: hidden;
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
  }
}
</style>

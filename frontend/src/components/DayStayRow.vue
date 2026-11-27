<script setup lang="ts">
import type { ScheduleDay } from '@/types/activity'
import type { Stay } from '@/types/stay'
import { bookingStatusMeta } from '@/utils/activityDetails'
import { nightNumber } from '@/utils/stays'

defineProps<{
  day: ScheduleDay
  kind: 'checkout' | 'tonight'
}>()

const emit = defineEmits<{
  addStay: [date: string]
  editStay: [stay: Stay]
  extendStay: [stay: Stay]
}>()
</script>

<template>
  <button
    v-if="kind === 'checkout' && day.checkingOut"
    type="button"
    class="stay-row checkout"
    data-testid="day-checkout"
    @click="emit('editStay', day.checkingOut)"
  >
    <span class="stay-icon" aria-hidden="true">🧳</span>
    <span class="stay-label">Check out</span>
    <span class="stay-name">{{ day.checkingOut.name }}</span>
  </button>
  <button
    v-else-if="kind === 'tonight' && day.stay"
    type="button"
    class="stay-row tonight"
    data-testid="day-stay"
    @click="emit('editStay', day.stay)"
  >
    <span class="stay-icon" aria-hidden="true">🌙</span>
    <span class="stay-label">Tonight</span>
    <span class="stay-name" data-testid="day-stay-name">{{ day.stay.name }}</span>
    <span class="stay-night">
      Night {{ nightNumber(day.stay, day.date) }} of {{ day.stay.nights }}
    </span>
    <span
      v-if="day.stay.bookingStatus !== 'NotNeeded'"
      class="booking-badge"
      :class="`tone-${bookingStatusMeta(day.stay.bookingStatus).tone}`"
    >
      {{ bookingStatusMeta(day.stay.bookingStatus).label }}
    </span>
  </button>
  <div
    v-else-if="kind === 'tonight' && day.checkingOut"
    class="stay-row missing quick"
    data-testid="day-no-stay"
  >
    <span class="stay-icon" aria-hidden="true">🌙</span>
    <span class="stay-label">Tonight</span>
    <span class="stay-name">No stay yet</span>
    <button
      type="button"
      class="extend-stay"
      data-testid="day-extend-stay"
      @click="emit('extendStay', day.checkingOut)"
    >
      Same as last night · {{ day.checkingOut.name }}
    </button>
    <button
      type="button"
      class="stay-action stay-action-button"
      data-testid="day-add-stay"
      @click="emit('addStay', day.date)"
    >
      + Add stay
    </button>
  </div>
  <button
    v-else-if="kind === 'tonight'"
    type="button"
    class="stay-row missing"
    data-testid="day-add-stay"
    @click="emit('addStay', day.date)"
  >
    <span class="stay-icon" aria-hidden="true">🌙</span>
    <span class="stay-label">Tonight</span>
    <span class="stay-name">No stay yet</span>
    <span class="stay-action">+ Add stay</span>
  </button>
</template>

<style scoped>
.stay-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  margin-top: 0.9rem;
  padding: 0.6rem 0.85rem;
  font: inherit;
  font-size: 0.88rem;
  text-align: left;
  color: var(--color-text);
  background: var(--color-surface-muted, rgb(245 241 234));
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md, 10px);
  cursor: pointer;
}

.stay-row:hover {
  border-color: var(--color-primary);
}

.stay-row.tonight {
  background: rgb(46 74 98 / 0.07);
  border-color: rgb(46 74 98 / 0.18);
}

.stay-row.missing {
  color: var(--color-muted);
  background: transparent;
  border-style: dashed;
}

.stay-row.checkout {
  margin-top: 0.6rem;
  margin-bottom: 0.2rem;
  background: transparent;
}

.stay-label {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-muted);
}

.stay-name {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.stay-night {
  font-size: 0.8rem;
  color: var(--color-muted);
}

.stay-action {
  margin-left: auto;
  font-weight: 600;
  color: var(--color-primary);
}

.stay-row.quick {
  cursor: default;
}

.stay-row.quick:hover {
  border-color: var(--color-border);
}

.extend-stay {
  max-width: 100%;
  padding: 0.25rem 0.7rem;
  overflow: hidden;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border: 1px solid rgb(31 92 86 / 0.25);
  border-radius: 999px;
  cursor: pointer;
}

.extend-stay:hover {
  color: #fff;
  background: var(--color-primary);
}

.stay-action-button {
  padding: 0.2rem 0.3rem;
  font: inherit;
  font-weight: 600;
  background: none;
  border: 0;
  border-radius: 6px;
  cursor: pointer;
}

.stay-action-button:hover {
  text-decoration: underline;
}
</style>

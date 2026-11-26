<script setup lang="ts">
import { ref } from 'vue'
import type { Activity, ScheduleDay } from '@/types/activity'
import { dateParts, formatDayLabel } from '@/utils/date'
import { formatCurrency } from '@/utils/money'
import { formatTime } from '@/utils/schedule'
import { bookingStatusMeta, categoryMeta } from '@/utils/activityDetails'
import { insertionIndex, shiftedStartTime, startTimeForDrop } from '@/utils/board'

export interface MoveRequest {
  activityId: string
  date: string
  startTime: string
}

const props = withDefaults(
  defineProps<{
    days: ScheduleDay[]
    movingId?: string | null
    today?: string | null
  }>(),
  {
    movingId: null,
    today: null,
  },
)

const emit = defineEmits<{
  move: [request: MoveRequest]
  edit: [activity: Activity]
  add: [date: string]
}>()

const MOVE_STEP_MINUTES = 30

const dragged = ref<Activity | null>(null)
const dropDate = ref<string | null>(null)
const dropIndex = ref<number | null>(null)
const menuFor = ref<string | null>(null)

function onDragStart(event: DragEvent, activity: Activity) {
  dragged.value = activity
  menuFor.value = null
  event.dataTransfer?.setData('text/plain', activity.id)
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

function onDragEnd() {
  dragged.value = null
  dropDate.value = null
  dropIndex.value = null
}

function onListDragOver(event: DragEvent, day: ScheduleDay) {
  if (!dragged.value) {
    return
  }

  event.preventDefault()
  const list = event.currentTarget as HTMLElement
  const midpoints = [...list.querySelectorAll<HTMLElement>('[data-card]')]
    .filter((card) => card.dataset.card !== dragged.value?.id)
    .map((card) => {
      const rect = card.getBoundingClientRect()
      return rect.top + rect.height / 2
    })

  dropDate.value = day.date
  dropIndex.value = day.activities.length === 0 ? null : insertionIndex(midpoints, event.clientY)
}

function onHeaderDragOver(event: DragEvent, day: ScheduleDay) {
  if (!dragged.value) {
    return
  }

  event.preventDefault()
  dropDate.value = day.date
  dropIndex.value = null
}

function onDrop(event: DragEvent, day: ScheduleDay) {
  event.preventDefault()
  const activity = dragged.value

  if (!activity) {
    return
  }

  const others = day.activities.filter((item) => item.id !== activity.id)
  const startTime =
    dropIndex.value === null
      ? formatTime(activity.startTime)
      : startTimeForDrop(others, dropIndex.value, activity)

  onDragEnd()

  if (day.date === activity.date && startTime === formatTime(activity.startTime)) {
    return
  }

  emit('move', { activityId: activity.id, date: day.date, startTime })
}

function dayIndexOf(date: string): number {
  return props.days.findIndex((day) => day.date === date)
}

function moveBy(activity: Activity, minutes: number) {
  menuFor.value = null
  emit('move', {
    activityId: activity.id,
    date: activity.date,
    startTime: shiftedStartTime(activity, minutes),
  })
}

function moveToDay(activity: Activity, offset: number) {
  const target = props.days[dayIndexOf(activity.date) + offset]
  menuFor.value = null

  if (target) {
    emit('move', {
      activityId: activity.id,
      date: target.date,
      startTime: formatTime(activity.startTime),
    })
  }
}

function toggleMenu(activityId: string) {
  menuFor.value = menuFor.value === activityId ? null : activityId
}
</script>

<template>
  <div class="board" data-testid="board-view">
    <section
      v-for="(day, dayIndex) in days"
      :key="day.date"
      class="column"
      :class="{ 'drop-target': dropDate === day.date, 'is-today': day.date === today }"
      :data-date="day.date"
      data-testid="board-column"
      :aria-label="`Day ${dayIndex + 1}, ${formatDayLabel(day.date)}`"
      @dragleave.self="dropDate = null"
    >
      <header
        class="column-header"
        data-testid="board-column-header"
        @dragover="onHeaderDragOver($event, day)"
        @drop="onDrop($event, day)"
      >
        <span class="column-day">
          <span class="eyebrow">
            Day {{ dayIndex + 1 }}
            <span v-if="day.date === today" class="today-badge" data-testid="today-badge"
              >Today</span
            >
          </span>
          <span class="column-date"
            >{{ dateParts(day.date).weekday }}, {{ dateParts(day.date).month }}
            {{ dateParts(day.date).day }}</span
          >
        </span>
        <span class="column-tools">
          <span class="column-count">{{ day.activities.length }}</span>
          <button
            type="button"
            class="column-add"
            :aria-label="`Add activity to Day ${dayIndex + 1}`"
            data-testid="board-add"
            @click="emit('add', day.date)"
          >
            +
          </button>
        </span>
      </header>

      <ol
        class="cards"
        data-testid="board-cards"
        @dragover="onListDragOver($event, day)"
        @drop="onDrop($event, day)"
      >
        <template v-for="(activity, index) in day.activities" :key="activity.id">
          <li
            v-if="dropDate === day.date && dropIndex === index"
            class="drop-indicator"
            data-testid="drop-indicator"
            aria-hidden="true"
          />
          <li
            class="board-card"
            :class="[
              `category-${activity.category.toLowerCase()}`,
              { dragging: dragged?.id === activity.id, moving: movingId === activity.id },
            ]"
            :data-card="activity.id"
            draggable="true"
            data-testid="board-card"
            @dragstart="onDragStart($event, activity)"
            @dragend="onDragEnd"
          >
            <span class="card-time" data-testid="board-card-time">
              {{ formatTime(activity.startTime) }} – {{ formatTime(activity.endTime) }}
            </span>
            <button
              type="button"
              class="card-name"
              data-testid="board-card-name"
              @click="emit('edit', activity)"
            >
              {{ activity.name }}
            </button>
            <span class="card-meta">
              <span class="card-category">
                {{ categoryMeta(activity.category).icon }}
                {{ categoryMeta(activity.category).label }}
              </span>
              <span
                v-if="activity.bookingStatus !== 'NotNeeded'"
                class="booking-badge"
                :class="`tone-${bookingStatusMeta(activity.bookingStatus).tone}`"
                data-testid="board-booking-badge"
              >
                {{ bookingStatusMeta(activity.bookingStatus).label }}
              </span>
            </span>
            <span v-if="activity.cost > 0" class="card-cost">
              {{ formatCurrency(activity.cost, activity.currency) }}
            </span>
            <div class="card-move">
              <button
                type="button"
                class="btn btn-ghost btn-small"
                :aria-expanded="menuFor === activity.id"
                :aria-label="`Move ${activity.name}`"
                data-testid="move-menu"
                @click="toggleMenu(activity.id)"
              >
                Move
              </button>
              <div v-if="menuFor === activity.id" class="popover-menu move-menu" role="menu">
                <button
                  type="button"
                  role="menuitem"
                  data-testid="move-earlier"
                  @click="moveBy(activity, -MOVE_STEP_MINUTES)"
                >
                  30 min earlier
                </button>
                <button
                  type="button"
                  role="menuitem"
                  data-testid="move-later"
                  @click="moveBy(activity, MOVE_STEP_MINUTES)"
                >
                  30 min later
                </button>
                <button
                  type="button"
                  role="menuitem"
                  data-testid="move-previous-day"
                  :disabled="dayIndex === 0"
                  @click="moveToDay(activity, -1)"
                >
                  Previous day
                </button>
                <button
                  type="button"
                  role="menuitem"
                  data-testid="move-next-day"
                  :disabled="dayIndex === days.length - 1"
                  @click="moveToDay(activity, 1)"
                >
                  Next day
                </button>
              </div>
            </div>
          </li>
        </template>
        <li
          v-if="
            dropDate === day.date &&
            dropIndex === day.activities.length &&
            day.activities.length > 0
          "
          class="drop-indicator"
          data-testid="drop-indicator"
          aria-hidden="true"
        />
        <li
          v-if="day.activities.length === 0"
          class="column-empty"
          data-testid="board-column-empty"
        >
          Drop an activity here
        </li>
      </ol>
    </section>
  </div>
</template>

<style scoped>
.board {
  display: grid;
  grid-auto-flow: column;
  grid-auto-columns: minmax(15rem, 1fr);
  gap: 0.85rem;
  overflow-x: auto;
  padding-bottom: 0.5rem;
  scroll-snap-type: x proximity;
}

.column {
  display: flex;
  flex-direction: column;
  min-height: 16rem;
  background: rgb(255 255 255 / 0.55);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  scroll-snap-align: start;
  transition:
    border-color 0.15s,
    background-color 0.15s;
}

.column.drop-target {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.column-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.85rem 1rem 0.6rem;
}

.column-day {
  display: flex;
  flex-direction: column;
}

.column-date {
  font-family: var(--font-display);
  font-weight: 700;
}

.column-tools {
  display: flex;
  align-items: center;
  gap: 0.35rem;
}

.column-add {
  display: grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--color-primary);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  cursor: pointer;
}

.column-add:hover {
  background: var(--color-primary-soft);
}

.column-count {
  min-width: 1.6rem;
  padding: 0.1rem 0.5rem;
  font-size: 0.75rem;
  font-weight: 700;
  text-align: center;
  color: var(--color-muted);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
}

.cards {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0;
  padding: 0 0.75rem 0.85rem;
  list-style: none;
}

.board-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  padding: 0.7rem 0.8rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-left: 3px solid var(--category-color, var(--color-primary));
  border-radius: 10px;
  box-shadow: var(--shadow-sm);
  cursor: grab;
  transition:
    opacity 0.15s,
    box-shadow 0.15s;
}

.board-card:hover {
  box-shadow: var(--shadow);
}

.board-card.dragging {
  opacity: 0.4;
}

.board-card.moving {
  opacity: 0.6;
}

.card-time {
  font-size: 0.78rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-muted);
}

.card-name {
  padding: 0;
  font-weight: 700;
  text-align: left;
  color: var(--color-text);
  background: none;
  border: none;
  cursor: pointer;
  overflow-wrap: anywhere;
}

.card-name:hover {
  color: var(--color-primary);
}

.card-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: var(--color-muted);
}

.card-cost {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-accent);
}

.card-move {
  position: absolute;
  top: 0.35rem;
  right: 0.35rem;
}

.card-move .btn {
  opacity: 0.5;
}

.board-card:hover .card-move .btn,
.card-move .btn:focus-visible,
.card-move .btn[aria-expanded='true'] {
  opacity: 1;
}

.move-menu {
  position: absolute;
  z-index: 20;
  top: calc(100% + 0.25rem);
  right: 0;
  display: flex;
  flex-direction: column;
  min-width: 10rem;
  padding: 0.3rem;
}

.move-menu button {
  padding: 0.5rem 0.7rem;
  font-size: 0.85rem;
  text-align: left;
  background: none;
  border: none;
  border-radius: 8px;
  cursor: pointer;
}

.move-menu button:hover:not(:disabled) {
  background: var(--color-primary-soft);
}

.move-menu button:disabled {
  color: var(--color-subtle);
  cursor: not-allowed;
}

.drop-indicator {
  height: 3px;
  margin: -0.15rem 0;
  background: var(--color-primary);
  border-radius: 2px;
}

.column-empty {
  display: grid;
  place-items: center;
  flex: 1;
  min-height: 5rem;
  font-size: 0.85rem;
  color: var(--color-subtle);
  border: 1.5px dashed var(--color-border);
  border-radius: 10px;
}
.column.is-today {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-ring);
}

.today-badge {
  display: inline-block;
  margin-left: 0.35rem;
  padding: 0.05rem 0.4rem;
  font-size: 0.64rem;
  letter-spacing: 0.06em;
  color: #fff;
  background: var(--color-primary);
  border-radius: 999px;
}
</style>

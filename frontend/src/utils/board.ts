import type { Activity } from '@/types/activity'
import { formatTime } from '@/utils/schedule'

const STEP_MINUTES = 5
const MINUTES_PER_DAY = 24 * 60

export function toMinutes(time: string): number {
  const [hours = 0, minutes = 0] = formatTime(time).split(':').map(Number)
  return hours * 60 + minutes
}

export function fromMinutes(total: number): string {
  const clamped = Math.min(Math.max(total, 0), MINUTES_PER_DAY - STEP_MINUTES)
  return `${String(Math.floor(clamped / 60)).padStart(2, '0')}:${String(clamped % 60).padStart(2, '0')}`
}

export function durationOf(activity: Activity): number {
  return toMinutes(activity.endTime) - toMinutes(activity.startTime)
}

export function startTimeForDrop(
  dayActivities: Activity[],
  index: number,
  dragged: Activity,
): string {
  const others = dayActivities.filter((activity) => activity.id !== dragged.id)
  const clampedIndex = Math.min(Math.max(index, 0), others.length)
  const above = others[clampedIndex - 1]
  const below = others[clampedIndex]

  if (above) {
    return fromMinutes(Math.ceil(toMinutes(above.endTime) / STEP_MINUTES) * STEP_MINUTES)
  }

  if (below) {
    const start = toMinutes(below.startTime) - durationOf(dragged)
    return fromMinutes(Math.floor(start / STEP_MINUTES) * STEP_MINUTES)
  }

  return formatTime(dragged.startTime)
}

export function shiftedStartTime(activity: Activity, minutes: number): string {
  return fromMinutes(toMinutes(activity.startTime) + minutes)
}

export function insertionIndex(cardMidpoints: number[], pointerY: number): number {
  const index = cardMidpoints.findIndex((midpoint) => pointerY < midpoint)
  return index === -1 ? cardMidpoints.length : index
}

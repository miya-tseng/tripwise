import type { Activity, ScheduleDay } from '@/types/activity'

export function formatTime(time: string): string {
  return time.slice(0, 5)
}

function byStartTime(a: Activity, b: Activity): number {
  return formatTime(a.startTime).localeCompare(formatTime(b.startTime))
}

export function withoutActivity(days: ScheduleDay[], activityId: string): ScheduleDay[] {
  return days.map((day) => ({
    ...day,
    activities: day.activities.filter((activity) => activity.id !== activityId),
  }))
}

export function withActivity(days: ScheduleDay[], activity: Activity): ScheduleDay[] {
  return withoutActivity(days, activity.id).map((day) =>
    day.date === activity.date
      ? { ...day, activities: [...day.activities, activity].sort(byStartTime) }
      : day,
  )
}

const LATEST_START_MINUTES = 23 * 60 + 50
const MINUTES_PER_DAY = 24 * 60
const TIME_STEP_MINUTES = 5

function toMinutes(time: string): number {
  const [hours = 0, minutes = 0] = formatTime(time).split(':').map(Number)
  return hours * 60 + minutes
}

function fromMinutes(total: number): string {
  const hours = Math.floor(total / 60)
  const minutes = total % 60
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
}

export function slotAfter(
  endTime: string,
  durationMinutes: number,
): { startTime: string; endTime: string } {
  const roundedStart = Math.ceil(toMinutes(endTime) / TIME_STEP_MINUTES) * TIME_STEP_MINUTES
  const start = Math.min(roundedStart, LATEST_START_MINUTES)
  const end = Math.min(
    start + Math.max(durationMinutes, TIME_STEP_MINUTES),
    MINUTES_PER_DAY - TIME_STEP_MINUTES,
  )

  return { startTime: fromMinutes(start), endTime: fromMinutes(end) }
}

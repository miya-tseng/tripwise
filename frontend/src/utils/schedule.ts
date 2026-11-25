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

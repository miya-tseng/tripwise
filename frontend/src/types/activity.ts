export interface Activity {
  id: string
  name: string
  date: string
  startTime: string
  endTime: string
  cost: number
}

export interface SaveActivityInput {
  name: string
  date: string
  startTime: string
  endTime: string
  cost: number
}

export interface ScheduleDay {
  date: string
  activities: Activity[]
}

export interface Schedule {
  tripId: string
  days: ScheduleDay[]
}

export const MAX_ACTIVITY_NAME_LENGTH = 100

export const MAX_COST = 9_999_999_999.99

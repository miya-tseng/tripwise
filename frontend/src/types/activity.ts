import type { Place } from './place'
import type { Stay } from './stay'

export type ActivityCategory =
  'Sightseeing' | 'Food' | 'Transport' | 'Shopping' | 'Experience' | 'Other'

export type BookingStatus = 'NotNeeded' | 'ToBook' | 'Booked' | 'Paid'

export interface ActivityDetails {
  category: ActivityCategory
  bookingStatus: BookingStatus
  notes: string
  confirmationCode: string
  link: string | null
}

export interface Activity extends ActivityDetails {
  id: string
  name: string
  date: string
  startTime: string
  endTime: string
  cost: number
  currency: string
  costInTwd: number | null
  place?: Place | null
}

export interface SaveActivityInput extends ActivityDetails {
  name: string
  date: string
  startTime: string
  endTime: string
  cost: number
  currency: string
  placeId?: string
  placeSessionToken?: string
}

export interface ScheduleDay {
  date: string
  activities: Activity[]
  stay?: Stay | null
  checkingOut?: Stay | null
}

export interface Schedule {
  tripId: string
  ratesAvailable: boolean
  ratesUpdatedAt: string | null
  stays?: Stay[]
  days: ScheduleDay[]
}

export const MAX_ACTIVITY_NAME_LENGTH = 100

export const MAX_COST = 9_999_999_999.99

export const MAX_NOTES_LENGTH = 2000
export const MAX_CONFIRMATION_CODE_LENGTH = 100

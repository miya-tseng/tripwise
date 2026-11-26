import type { BookingStatus } from './activity'
import type { Place } from './place'

export interface Stay {
  id: string
  name: string
  checkIn: string
  checkOut: string
  nights: number
  cost: number
  currency: string
  costInTwd: number | null
  place?: Place | null
  bookingStatus: BookingStatus
  confirmationCode: string
}

export interface SaveStayInput {
  name: string
  checkIn: string
  checkOut: string
  cost: number
  currency: string
  bookingStatus: BookingStatus
  confirmationCode: string
  placeId?: string
  placeSessionToken?: string
}

export const MAX_STAY_NAME_LENGTH = 150

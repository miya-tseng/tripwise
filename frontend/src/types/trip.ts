import type { Place } from './place'

export interface Trip {
  id: string
  name: string
  startDate: string
  endDate: string
  days: number
  destination?: Place | null
}

export interface CreateTripInput {
  name: string
  startDate: string
  endDate: string
  destinationPlaceId?: string | null
  placeSessionToken?: string
}

export type UpdateTripInput = CreateTripInput

export const MAX_TRIP_DAYS = 30

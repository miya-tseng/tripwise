export interface Trip {
  id: string
  name: string
  startDate: string
  endDate: string
  days: number
}

export interface CreateTripInput {
  name: string
  startDate: string
  endDate: string
}

export type UpdateTripInput = CreateTripInput

export const MAX_TRIP_DAYS = 30

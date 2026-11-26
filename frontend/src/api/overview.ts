import type { TripOverview } from '@/types/overview'
import { request } from './http'

export async function fetchOverview(tripId: string): Promise<TripOverview> {
  const response = await request(`/api/trips/${tripId}/overview`)
  return response.json()
}

import type { GeoPoint, PlaceSuggestion } from '@/types/place'
import { request } from './http'

export async function searchPlaces(
  query: string,
  sessionToken: string,
  near: GeoPoint | null,
): Promise<PlaceSuggestion[]> {
  const params = new URLSearchParams({ query, sessionToken })

  if (near) {
    params.set('lat', String(near.latitude))
    params.set('lng', String(near.longitude))
  }

  const response = await request(`/api/places/autocomplete?${params}`)
  return response.json()
}

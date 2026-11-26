import type { Activity } from '@/types/activity'
import type { Place } from '@/types/place'

const MAPS_DIRECTIONS_URL = 'https://www.google.com/maps/dir/'
const MAX_ROUTE_WAYPOINTS = 9

function coordinates(place: Place): string {
  return `${place.latitude},${place.longitude}`
}

export function placeLink(place: Place): string {
  return place.googleMapsUri
}

export function directionsLink(from: Place, to: Place): string {
  const params = new URLSearchParams({
    api: '1',
    origin: coordinates(from),
    origin_place_id: from.placeId,
    destination: coordinates(to),
    destination_place_id: to.placeId,
  })

  return `${MAPS_DIRECTIONS_URL}?${params}`
}

export function dayRouteLink(places: Place[]): string | null {
  if (places.length < 2) {
    return null
  }

  const origin = places[0]!
  const destination = places[places.length - 1]!
  const waypoints = places.slice(1, -1).slice(0, MAX_ROUTE_WAYPOINTS)
  const params = new URLSearchParams({
    api: '1',
    origin: coordinates(origin),
    origin_place_id: origin.placeId,
    destination: coordinates(destination),
    destination_place_id: destination.placeId,
  })

  if (waypoints.length > 0) {
    params.set('waypoints', waypoints.map(coordinates).join('|'))
    params.set('waypoint_place_ids', waypoints.map((place) => place.placeId).join('|'))
  }

  return `${MAPS_DIRECTIONS_URL}?${params}`
}

export function placesOf(activities: Activity[]): Place[] {
  return activities.flatMap((activity) => activity.place ?? [])
}

export function previousPlaceOf(activities: Activity[], index: number): Place | null {
  for (let previous = index - 1; previous >= 0; previous--) {
    const place = activities[previous]?.place

    if (place) {
      return place
    }
  }

  return null
}

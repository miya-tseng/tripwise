export interface Place {
  placeId: string
  name: string
  address: string
  latitude: number
  longitude: number
  googleMapsUri: string
}

export interface PlaceSuggestion {
  placeId: string
  name: string
  description: string
}

export interface GeoPoint {
  latitude: number
  longitude: number
}

export type PlaceSearchKind = 'any' | 'destination'

export const MIN_PLACE_QUERY_LENGTH = 2

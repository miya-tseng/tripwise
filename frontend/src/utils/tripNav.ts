export type TripSection = 'itinerary' | 'overview' | 'checklist'

export interface TripNavItem {
  section: TripSection
  label: string
  icon: string
  routeName: string
}

export const TRIP_NAV: TripNavItem[] = [
  { section: 'itinerary', label: 'Itinerary', icon: '🗓', routeName: 'trip-schedule' },
  { section: 'overview', label: 'Overview', icon: '📊', routeName: 'trip-overview' },
  { section: 'checklist', label: 'Checklist', icon: '✅', routeName: 'trip-checklist' },
]

export function sectionOfRoute(routeName: unknown): TripSection | null {
  return TRIP_NAV.find((item) => item.routeName === routeName)?.section ?? null
}

export function tripRoute(section: TripSection, tripId: string) {
  const item = TRIP_NAV.find((entry) => entry.section === section)!
  return { name: item.routeName, params: { id: tripId } }
}

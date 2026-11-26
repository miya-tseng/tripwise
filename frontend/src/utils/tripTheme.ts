import type { Trip } from '@/types/trip'
import type { TripGroups } from '@/utils/trips'

export interface TripCover {
  from: string
  to: string
}

export const COVERS: TripCover[] = [
  { from: '#2a6b5f', to: '#7fb8a4' },
  { from: '#c9714b', to: '#f2c28f' },
  { from: '#3f5a8a', to: '#9db4dd' },
  { from: '#a14f6b', to: '#eab0c0' },
  { from: '#6b6a2f', to: '#c9c58a' },
  { from: '#2f6f8f', to: '#93cde0' },
]

const FLAGS: Record<string, string> = {
  japan: '🇯🇵',
  日本: '🇯🇵',
  taiwan: '🇹🇼',
  台灣: '🇹🇼',
  臺灣: '🇹🇼',
  'south korea': '🇰🇷',
  korea: '🇰🇷',
  南韓: '🇰🇷',
  韓國: '🇰🇷',
  thailand: '🇹🇭',
  泰國: '🇹🇭',
  china: '🇨🇳',
  中國: '🇨🇳',
  'hong kong': '🇭🇰',
  香港: '🇭🇰',
  singapore: '🇸🇬',
  新加坡: '🇸🇬',
  vietnam: '🇻🇳',
  越南: '🇻🇳',
  'united states': '🇺🇸',
  usa: '🇺🇸',
  美國: '🇺🇸',
  'united kingdom': '🇬🇧',
  uk: '🇬🇧',
  英國: '🇬🇧',
  france: '🇫🇷',
  法國: '🇫🇷',
  italy: '🇮🇹',
  義大利: '🇮🇹',
  germany: '🇩🇪',
  德國: '🇩🇪',
  spain: '🇪🇸',
  西班牙: '🇪🇸',
  australia: '🇦🇺',
  澳洲: '🇦🇺',
  canada: '🇨🇦',
  加拿大: '🇨🇦',
}

function hash(text: string): number {
  let value = 0

  for (const char of text) {
    value = (value * 31 + char.codePointAt(0)!) >>> 0
  }

  return value
}

export function coverFor(trip: Pick<Trip, 'name' | 'destination'>): TripCover {
  const key = trip.destination?.placeId ?? trip.name
  return COVERS[hash(key) % COVERS.length]!
}

export function coverStyle(cover: TripCover): Record<string, string> {
  return { '--cover-from': cover.from, '--cover-to': cover.to }
}

export function countryOf(trip: Pick<Trip, 'destination'>): string | null {
  const destination = trip.destination

  if (!destination) {
    return null
  }

  const parts = [destination.address, destination.name]
    .flatMap((text) => text.split(/[,，]/))
    .map((part) => part.trim())
    .filter((part) => part !== '')

  return [...parts].reverse().find((part) => FLAGS[part.toLowerCase()] !== undefined) ?? null
}

export function flagFor(trip: Pick<Trip, 'destination'>): string | null {
  const country = countryOf(trip)
  return country ? FLAGS[country.toLowerCase()]! : null
}

export function greeting(now: Date = new Date()): string {
  const hour = now.getHours()

  if (hour < 5 || hour >= 18) {
    return 'Good evening'
  }

  return hour < 12 ? 'Good morning' : 'Good afternoon'
}

export function tripsSummary(groups: TripGroups): string {
  const parts = [
    groups.ongoing.length > 0 ? `${groups.ongoing.length} happening now` : '',
    groups.upcoming.length > 0 ? `${groups.upcoming.length} coming up` : '',
  ].filter((part) => part !== '')

  if (parts.length === 0) {
    return groups.past.length > 0 ? 'Time to plan the next adventure.' : 'Where to next?'
  }

  return parts.join(' · ')
}

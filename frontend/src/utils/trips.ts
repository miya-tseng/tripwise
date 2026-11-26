import type { Trip } from '@/types/trip'
import { countDays } from '@/utils/date'

export type TripPhase = 'ongoing' | 'upcoming' | 'past'

export interface TripGroups {
  ongoing: Trip[]
  upcoming: Trip[]
  past: Trip[]
}

export function todayIso(now: Date = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

export function tripPhase(trip: Trip, today: string): TripPhase {
  if (trip.endDate < today) {
    return 'past'
  }

  return trip.startDate > today ? 'upcoming' : 'ongoing'
}

export function groupTrips(trips: Trip[], today: string): TripGroups {
  const groups: TripGroups = { ongoing: [], upcoming: [], past: [] }

  for (const trip of trips) {
    groups[tripPhase(trip, today)].push(trip)
  }

  groups.ongoing.sort((a, b) => a.startDate.localeCompare(b.startDate))
  groups.upcoming.sort((a, b) => a.startDate.localeCompare(b.startDate))
  groups.past.sort((a, b) => b.endDate.localeCompare(a.endDate))
  return groups
}

export function currentDayNumber(trip: Trip, today: string): number {
  return countDays(trip.startDate, today)
}

export function progressLabel(trip: Trip, today: string): string {
  return `Day ${currentDayNumber(trip, today)} of ${trip.days}`
}

export function countdownLabel(trip: Trip, today: string): string {
  const days = countDays(today, trip.startDate) - 1

  if (days === 1) {
    return 'Tomorrow'
  }

  if (days < 14) {
    return `In ${days} days`
  }

  if (days < 60) {
    return `In ${Math.floor(days / 7)} weeks`
  }

  return `In ${Math.round(days / 30)} months`
}

export function daysLabel(days: number): string {
  return `${days} ${days === 1 ? 'day' : 'days'}`
}

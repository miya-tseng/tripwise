import type { Stay } from '@/types/stay'
import { addDays, countDays, formatDayLabel } from './date'

export function nightsLabel(nights: number): string {
  return nights === 1 ? '1 night' : `${nights} nights`
}

export function stayRangeLabel(stay: Pick<Stay, 'checkIn' | 'checkOut'>): string {
  return `${formatDayLabel(stay.checkIn)} → ${formatDayLabel(stay.checkOut)}`
}

export function nightNumber(stay: Stay, date: string): number {
  return countDays(stay.checkIn, date)
}

export function checkOutDates(dates: string[]): string[] {
  const last = dates.at(-1)
  return last ? [...dates.slice(1), addDays(last, 1)] : []
}

export function firstFreeNight(dates: string[], stays: Stay[]): string | null {
  return (
    dates.find((date) => !stays.some((stay) => stay.checkIn <= date && date < stay.checkOut)) ??
    null
  )
}

export function nextCheckInAfter(date: string, stays: Stay[]): string | null {
  const later = stays.map((stay) => stay.checkIn).filter((checkIn) => checkIn > date)
  return later.length > 0 ? later.reduce((a, b) => (a < b ? a : b)) : null
}

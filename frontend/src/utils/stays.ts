import type { SaveStayInput, Stay } from '@/types/stay'
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

export function stayInput(stay: Stay, changes: Partial<SaveStayInput> = {}): SaveStayInput {
  return {
    name: stay.name,
    checkIn: stay.checkIn,
    checkOut: stay.checkOut,
    cost: stay.cost,
    currency: stay.currency,
    bookingStatus: stay.bookingStatus,
    confirmationCode: stay.confirmationCode,
    ...(stay.place ? { placeId: stay.place.placeId } : {}),
    ...changes,
  }
}

export function extendedByOneNight(stay: Stay): SaveStayInput {
  const totalCents = Math.round(stay.cost * 100)
  const perNightCents = Math.round(totalCents / stay.nights)

  return stayInput(stay, {
    checkOut: addDays(stay.checkOut, 1),
    cost: (totalCents + perNightCents) / 100,
  })
}

export interface QuickCheckOut {
  label: string
  checkOut: string
}

export function quickCheckOuts(checkIn: string, lastCheckOut: string): QuickCheckOut[] {
  if (!checkIn || lastCheckOut <= checkIn) {
    return []
  }

  const maxNights = countDays(checkIn, lastCheckOut) - 1
  const choices: QuickCheckOut[] = [1, 2, 3]
    .filter((nights) => nights < maxNights)
    .map((nights) => ({ label: nightsLabel(nights), checkOut: addDays(checkIn, nights) }))

  const allLabel = maxNights === 1 ? nightsLabel(1) : `All ${nightsLabel(maxNights)}`
  return [...choices, { label: allLabel, checkOut: lastCheckOut }]
}

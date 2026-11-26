import type { AttentionItem, DaySpend } from '@/types/overview'
import { countDays, formatDayLabel } from '@/utils/date'

export interface AttentionView {
  icon: string
  title: string
  detail: string
  dayNumber: number
}

export function attentionView(item: AttentionItem, startDate: string): AttentionView {
  const dayNumber = countDays(startDate, item.date)
  const day = `Day ${dayNumber} · ${formatDayLabel(item.date)}`

  switch (item.kind) {
    case 'ActivityToBook':
      return { icon: '🎟️', title: `Book ${item.name}`, detail: day, dayNumber }
    case 'StayToBook':
      return { icon: '🛏', title: `Book ${item.name}`, detail: `Check-in ${day}`, dayNumber }
    case 'NightWithoutStay':
      return { icon: '🌙', title: 'No stay for the night', detail: day, dayNumber }
    case 'EmptyDay':
      return { icon: '📅', title: 'Nothing planned', detail: day, dayNumber }
  }
}

const NICE_STEPS = [1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10]

export function niceMax(value: number, tickCount = 4): number {
  if (value <= 0) {
    return 0
  }

  const rawStep = value / tickCount
  const magnitude = 10 ** Math.floor(Math.log10(rawStep))
  const step = NICE_STEPS.find((candidate) => candidate * magnitude >= rawStep) ?? 10
  return step * magnitude * tickCount
}

export function axisTicks(max: number, count = 4): number[] {
  if (max <= 0) {
    return [0]
  }

  return Array.from({ length: count + 1 }, (_, index) => (max / count) * index)
}

export function percentOf(value: number | null, max: number): number {
  return max > 0 && value ? Math.min((value / max) * 100, 100) : 0
}

export function dayMax(days: DaySpend[]): number {
  return niceMax(Math.max(0, ...days.map((day) => day.totalInTwd ?? 0)))
}

const compactFormatter = new Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

export function compactTwd(amount: number): string {
  return `NT$${compactFormatter.format(amount)}`
}

export function share(part: number | null, total: number | null): number {
  return part !== null && total ? Math.round((part / total) * 100) : 0
}

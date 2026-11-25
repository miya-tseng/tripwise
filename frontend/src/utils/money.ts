import { MAX_COST, type ScheduleDay } from '@/types/activity'

const moneyFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const costPattern = /^\d+(\.\d{1,2})?$/

export function formatMoney(amount: number): string {
  return moneyFormatter.format(amount)
}

function toCents(amount: number): number {
  return Math.round(amount * 100)
}

export function sumMoney(amounts: number[]): number {
  return amounts.reduce((total, amount) => total + toCents(amount), 0) / 100
}

export function dayTotal(day: ScheduleDay): number {
  return sumMoney(day.activities.map((activity) => activity.cost))
}

export function tripTotal(days: ScheduleDay[]): number {
  return sumMoney(days.map(dayTotal))
}

export type CostParseResult = { ok: true; value: number } | { ok: false; error: string }

export function parseCost(input: string): CostParseResult {
  const normalized = input.replace(/,/g, '').trim()

  if (normalized === '') {
    return { ok: true, value: 0 }
  }

  if (!costPattern.test(normalized)) {
    return { ok: false, error: 'Cost must be a positive amount with up to 2 decimal places.' }
  }

  const value = Number(normalized)

  if (value > MAX_COST) {
    return { ok: false, error: `Cost cannot exceed ${formatMoney(MAX_COST)}.` }
  }

  return { ok: true, value }
}

import { MAX_COST, type Activity, type ScheduleDay } from '@/types/activity'
import { HOME_CURRENCY, type CurrencyAmount, type ExchangeRates } from '@/types/currency'

const costPattern = /^\d+(\.\d{1,2})?$/
const currencyFormatters = new Map<string, Intl.NumberFormat>()
const numberFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})
const currencyNames = new Intl.DisplayNames(['en'], { type: 'currency' })

function currencyFormatter(currency: string): Intl.NumberFormat {
  let formatter = currencyFormatters.get(currency)

  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency,
        currencyDisplay: 'symbol',
        maximumFractionDigits: 2,
        ...(currency === HOME_CURRENCY ? { minimumFractionDigits: 2 } : {}),
      })
    } catch {
      formatter = numberFormatter
    }
    currencyFormatters.set(currency, formatter)
  }

  return formatter
}

export function formatCurrency(amount: number, currency: string): string {
  const formatter = currencyFormatter(currency)
  const formatted = formatter.format(amount)
  return formatter === numberFormatter ? `${formatted} ${currency}` : formatted
}

export function formatTwd(amount: number): string {
  return formatCurrency(amount, HOME_CURRENCY)
}

export function currencyName(currency: string): string {
  try {
    return currencyNames.of(currency) ?? currency
  } catch {
    return currency
  }
}

function toCents(amount: number): number {
  return Math.round(amount * 100)
}

export function sumMoney(amounts: number[]): number {
  return amounts.reduce((total, amount) => total + toCents(amount), 0) / 100
}

function totalInTwd(activities: Activity[]): number | null {
  const converted = activities.map((activity) => activity.costInTwd)
  return converted.every((amount): amount is number => amount !== null) ? sumMoney(converted) : null
}

export function dayTotalInTwd(day: ScheduleDay): number | null {
  return totalInTwd(day.activities)
}

export function tripTotalInTwd(days: ScheduleDay[]): number | null {
  return totalInTwd(days.flatMap((day) => day.activities))
}

export function totalsByCurrency(activities: Activity[]): CurrencyAmount[] {
  const totals = new Map<string, number[]>()

  for (const activity of activities) {
    totals.set(activity.currency, [...(totals.get(activity.currency) ?? []), activity.cost])
  }

  return [...totals.entries()]
    .map(([currency, amounts]) => ({ currency, amount: sumMoney(amounts) }))
    .sort((a, b) => {
      if (a.currency === HOME_CURRENCY) return -1
      if (b.currency === HOME_CURRENCY) return 1
      return a.currency.localeCompare(b.currency)
    })
}

export function formatTotals(totals: CurrencyAmount[]): string {
  return totals.length === 0
    ? formatTwd(0)
    : totals.map((total) => formatCurrency(total.amount, total.currency)).join(' + ')
}

export function toTwd(
  amount: number,
  currency: string,
  rates: ExchangeRates | null,
): number | null {
  if (currency === HOME_CURRENCY) {
    return amount
  }

  const unitsPerTwd = rates?.available ? rates.unitsPerBaseCurrency[currency] : undefined

  return unitsPerTwd ? Math.round((amount / unitsPerTwd) * 100) / 100 : null
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
    return { ok: false, error: `Cost cannot exceed ${numberFormatter.format(MAX_COST)}.` }
  }

  return { ok: true, value }
}

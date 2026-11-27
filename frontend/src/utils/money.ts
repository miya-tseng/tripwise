import { MAX_COST, type ScheduleDay } from '@/types/activity'
import type { Stay } from '@/types/stay'
import { countDays } from './date'
import {
  COMMON_CURRENCIES,
  HOME_CURRENCY,
  type CurrencyAmount,
  type ExchangeRates,
} from '@/types/currency'

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

export interface CostItem {
  cost: number
  currency: string
  costInTwd: number | null
}

function splitForNight(total: number, nights: number, nightIndex: number): number {
  const totalCents = toCents(total)
  const perNight = Math.floor(totalCents / nights)
  const isLastNight = nightIndex === nights - 1
  return (isLastNight ? totalCents - perNight * (nights - 1) : perNight) / 100
}

export function stayCostForNight(stay: Stay, date: string): CostItem {
  const index = countDays(stay.checkIn, date) - 1

  if (index < 0 || index >= stay.nights) {
    return { cost: 0, currency: stay.currency, costInTwd: 0 }
  }

  return {
    cost: splitForNight(stay.cost, stay.nights, index),
    currency: stay.currency,
    costInTwd: stay.costInTwd === null ? null : splitForNight(stay.costInTwd, stay.nights, index),
  }
}

export function dayCosts(day: ScheduleDay): CostItem[] {
  const stay = day.stay && day.stay.cost > 0 ? [stayCostForNight(day.stay, day.date)] : []
  return [...day.activities, ...stay]
}

export function tripCosts(days: ScheduleDay[], stays: Stay[] = []): CostItem[] {
  return [...days.flatMap((day) => day.activities), ...stays.filter((stay) => stay.cost > 0)]
}

export function totalInTwd(items: CostItem[]): number | null {
  const converted = items.map((item) => item.costInTwd)
  return converted.every((amount): amount is number => amount !== null) ? sumMoney(converted) : null
}

export function dayTotalInTwd(day: ScheduleDay): number | null {
  return totalInTwd(dayCosts(day))
}

export function tripTotalInTwd(days: ScheduleDay[], stays: Stay[] = []): number | null {
  return totalInTwd(tripCosts(days, stays))
}

export function totalsByCurrency(items: CostItem[]): CurrencyAmount[] {
  const totals = new Map<string, number[]>()

  for (const item of items) {
    totals.set(item.currency, [...(totals.get(item.currency) ?? []), item.cost])
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

export function currencySelectOptions(
  current: string,
  rates: ExchangeRates | null,
): { value: string; label: string; hint: string }[] {
  const available = rates?.available ? new Set(rates.currencies) : null
  const codes: string[] = COMMON_CURRENCIES.filter((code) => !available || available.has(code))
  const withCurrent = codes.includes(current) ? codes : [...codes, current]

  return withCurrent.map((code) => ({ value: code, label: code, hint: currencyName(code) }))
}

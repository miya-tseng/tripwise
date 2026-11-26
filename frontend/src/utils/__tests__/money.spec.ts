import { describe, expect, it } from 'vitest'
import type { Activity, ScheduleDay } from '@/types/activity'
import type { ExchangeRates } from '@/types/currency'
import {
  currencyName,
  dayTotalInTwd,
  formatCurrency,
  formatTotals,
  formatTwd,
  parseCost,
  sumMoney,
  toTwd,
  totalsByCurrency,
  tripTotalInTwd,
} from '../money'

function activity(cost: number, currency = 'TWD', costInTwd: number | null = cost): Activity {
  return {
    id: `${cost}-${currency}`,
    name: 'x',
    date: '2026-12-01',
    startTime: '10:00',
    endTime: '11:00',
    cost,
    currency,
    costInTwd,
  }
}

function day(date: string, activities: Activity[]): ScheduleDay {
  return { date, activities }
}

const rates: ExchangeRates = {
  baseCurrency: 'TWD',
  available: true,
  updatedAt: '2026-01-01T00:00:00Z',
  currencies: ['JPY', 'TWD', 'USD'],
  unitsPerBaseCurrency: { TWD: 1, JPY: 4.8, USD: 0.032 },
}

describe('formatCurrency', () => {
  it.each([
    [0, 'TWD', 'NT$0.00'],
    [2200.5, 'TWD', 'NT$2,200.50'],
    [3000, 'JPY', '¥3,000'],
    [12.5, 'USD', '$12.50'],
    [12, 'USD', '$12.00'],
    [9.99, 'EUR', '€9.99'],
  ])('formats %d %s as %s', (amount, currency, expected) => {
    expect(formatCurrency(amount, currency)).toBe(expected)
  })

  it('falls back to the code for unknown currencies', () => {
    expect(formatCurrency(5, 'ZZZ')).toMatch(/5.*ZZZ|ZZZ.*5/)
  })

  it('formats TWD with two decimals', () => {
    expect(formatTwd(615)).toBe('NT$615.00')
  })
})

describe('currencyName', () => {
  it('returns the English name of a currency', () => {
    expect(currencyName('JPY')).toBe('Japanese Yen')
  })
})

describe('sumMoney', () => {
  it('adds amounts without floating point errors', () => {
    expect(sumMoney([0.1, 0.2])).toBe(0.3)
  })

  it('returns zero for no amounts', () => {
    expect(sumMoney([])).toBe(0)
  })
})

describe('totals in TWD', () => {
  const days = [
    day('2026-12-01', [activity(3000, 'JPY', 625), activity(150)]),
    day('2026-12-02', []),
    day('2026-12-03', [activity(10, 'USD', 312.5)]),
  ]

  it('adds the converted costs of a day', () => {
    expect(days.map(dayTotalInTwd)).toEqual([775, 0, 312.5])
  })

  it('adds every day of the trip', () => {
    expect(tripTotalInTwd(days)).toBe(1087.5)
  })

  it('is unknown when a cost could not be converted', () => {
    const unconverted = [day('2026-12-01', [activity(3000, 'JPY', null), activity(150)])]

    expect(dayTotalInTwd(unconverted[0]!)).toBeNull()
    expect(tripTotalInTwd(unconverted)).toBeNull()
  })
})

describe('totalsByCurrency', () => {
  it('groups costs per currency with TWD first', () => {
    const totals = totalsByCurrency([
      activity(10, 'USD'),
      activity(1000, 'JPY'),
      activity(100),
      activity(2000, 'JPY'),
    ])

    expect(totals).toEqual([
      { currency: 'TWD', amount: 100 },
      { currency: 'JPY', amount: 3000 },
      { currency: 'USD', amount: 10 },
    ])
  })

  it('formats totals joined with plus signs', () => {
    expect(formatTotals(totalsByCurrency([activity(100), activity(3000, 'JPY')]))).toBe(
      'NT$100.00 + ¥3,000',
    )
  })

  it('formats no totals as zero TWD', () => {
    expect(formatTotals([])).toBe('NT$0.00')
  })
})

describe('toTwd', () => {
  it.each([
    [3000, 'JPY', 625],
    [2200, 'JPY', 458.33],
    [10, 'USD', 312.5],
    [150, 'TWD', 150],
  ])('converts %d %s to %d TWD', (amount, currency, expected) => {
    expect(toTwd(amount, currency, rates)).toBe(expected)
  })

  it('returns null for an unknown currency', () => {
    expect(toTwd(10, 'EUR', rates)).toBeNull()
  })

  it('returns null when rates are unavailable', () => {
    expect(toTwd(10, 'JPY', { ...rates, available: false })).toBeNull()
    expect(toTwd(10, 'JPY', null)).toBeNull()
  })

  it('keeps TWD even without rates', () => {
    expect(toTwd(10, 'TWD', null)).toBe(10)
  })
})

describe('parseCost', () => {
  it.each([
    ['', 0],
    ['  ', 0],
    ['0', 0],
    ['2200', 2200],
    ['2200.5', 2200.5],
    ['2,200.50', 2200.5],
    [' 15.25 ', 15.25],
  ])('parses %j as %d', (input, value) => {
    expect(parseCost(input)).toEqual({ ok: true, value })
  })

  it.each(['-1', '12.345', 'abc', '1.2.3', '.5', '5.'])('rejects %j', (input) => {
    expect(parseCost(input)).toEqual({
      ok: false,
      error: 'Cost must be a positive amount with up to 2 decimal places.',
    })
  })

  it('rejects amounts over the maximum', () => {
    expect(parseCost('10000000000')).toEqual({
      ok: false,
      error: 'Cost cannot exceed 9,999,999,999.99.',
    })
  })
})

import { describe, expect, it } from 'vitest'
import type { Activity, ScheduleDay } from '@/types/activity'
import { dayTotal, formatMoney, parseCost, sumMoney, tripTotal } from '../money'

function activity(cost: number): Activity {
  return {
    id: String(cost),
    name: 'x',
    date: '2026-12-01',
    startTime: '10:00',
    endTime: '11:00',
    cost,
  }
}

function day(date: string, costs: number[]): ScheduleDay {
  return { date, activities: costs.map(activity) }
}

describe('formatMoney', () => {
  it.each([
    [0, '0.00'],
    [5, '5.00'],
    [12.5, '12.50'],
    [2200.5, '2,200.50'],
    [1234567.89, '1,234,567.89'],
  ])('formats %d as %s', (amount, expected) => {
    expect(formatMoney(amount)).toBe(expected)
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

describe('totals', () => {
  const days = [
    day('2026-12-01', [1500.5, 0, 2200]),
    day('2026-12-02', []),
    day('2026-12-03', [3800.25]),
  ]

  it('adds the costs of a day', () => {
    expect(days.map(dayTotal)).toEqual([3700.5, 0, 3800.25])
  })

  it('adds every day of the trip', () => {
    expect(tripTotal(days)).toBe(7500.75)
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

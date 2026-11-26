import { describe, expect, it } from 'vitest'
import type { Stay } from '@/types/stay'
import {
  checkOutDates,
  firstFreeNight,
  nextCheckInAfter,
  nightNumber,
  nightsLabel,
  stayRangeLabel,
} from '../stays'
import {
  dayTotalInTwd,
  stayCostForNight,
  totalsByCurrency,
  tripCosts,
  tripTotalInTwd,
} from '../money'

function stay(overrides: Partial<Stay> = {}): Stay {
  return {
    id: 'hotel',
    name: 'Hotel Gracery',
    checkIn: '2026-12-01',
    checkOut: '2026-12-04',
    nights: 3,
    cost: 100,
    currency: 'TWD',
    costInTwd: 100,
    bookingStatus: 'Booked',
    confirmationCode: '',
    ...overrides,
  }
}

const dates = ['2026-12-01', '2026-12-02', '2026-12-03', '2026-12-04']

describe('stay helpers', () => {
  it('labels nights', () => {
    expect(nightsLabel(1)).toBe('1 night')
    expect(nightsLabel(3)).toBe('3 nights')
  })

  it('labels the date range', () => {
    expect(stayRangeLabel(stay())).toBe('Tue, Dec 1 → Fri, Dec 4')
  })

  it('numbers each night from check-in', () => {
    expect(nightNumber(stay(), '2026-12-01')).toBe(1)
    expect(nightNumber(stay(), '2026-12-03')).toBe(3)
  })

  it('allows checking out the day after the trip', () => {
    expect(checkOutDates(dates)).toEqual(['2026-12-02', '2026-12-03', '2026-12-04', '2026-12-05'])
  })

  it('finds the first night without a stay', () => {
    expect(firstFreeNight(dates, [stay()])).toBe('2026-12-04')
    expect(firstFreeNight(dates, [stay({ checkOut: '2026-12-05', nights: 4 })])).toBeNull()
  })

  it('finds the next check-in after a date', () => {
    const ryokan = stay({ id: 'ryokan', checkIn: '2026-12-03', checkOut: '2026-12-05' })
    expect(nextCheckInAfter('2026-12-01', [ryokan])).toBe('2026-12-03')
    expect(nextCheckInAfter('2026-12-03', [ryokan])).toBeNull()
  })
})

describe('stay costs', () => {
  it('splits the cost per night and gives the remainder to the last night', () => {
    expect(stayCostForNight(stay(), '2026-12-01').cost).toBe(33.33)
    expect(stayCostForNight(stay(), '2026-12-03').cost).toBe(33.34)
    expect(stayCostForNight(stay(), '2026-12-04').cost).toBe(0)
  })

  it('adds the nightly share to the day total', () => {
    const day = { date: '2026-12-01', activities: [], stay: stay({ cost: 9000, costInTwd: 9000 }) }
    expect(dayTotalInTwd(day)).toBe(3000)
  })

  it('adds stays to the trip total', () => {
    expect(tripTotalInTwd([], [stay()])).toBe(100)
  })

  it('cannot convert a stay without a TWD amount', () => {
    const ryokan = stay({ currency: 'JPY', cost: 20000, costInTwd: null })
    expect(tripTotalInTwd([], [ryokan])).toBeNull()
    expect(totalsByCurrency(tripCosts([], [ryokan]))).toEqual([{ currency: 'JPY', amount: 20000 }])
  })

  it('ignores free stays', () => {
    expect(tripCosts([], [stay({ cost: 0, costInTwd: 0 })])).toEqual([])
  })
})

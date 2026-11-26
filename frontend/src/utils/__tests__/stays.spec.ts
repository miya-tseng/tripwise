import { describe, expect, it } from 'vitest'
import type { Stay } from '@/types/stay'
import {
  checkOutDates,
  extendedByOneNight,
  firstFreeNight,
  nextCheckInAfter,
  nightNumber,
  nightsLabel,
  quickCheckOuts,
  stayInput,
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

describe('quick stay helpers', () => {
  it('copies a stay into a save input with its place', () => {
    const place = {
      placeId: 'gracery',
      name: 'Hotel Gracery',
      address: 'Shinjuku',
      latitude: 35.69,
      longitude: 139.7,
      googleMapsUri: 'https://maps.google.com/?cid=9',
    }

    expect(stayInput(stay({ place }))).toEqual({
      name: 'Hotel Gracery',
      checkIn: '2026-12-01',
      checkOut: '2026-12-04',
      cost: 100,
      currency: 'TWD',
      bookingStatus: 'Booked',
      confirmationCode: '',
      placeId: 'gracery',
    })
  })

  it('extends a stay by one night at the same nightly price', () => {
    const input = extendedByOneNight(stay({ cost: 9000, nights: 2, checkOut: '2026-12-03' }))

    expect(input.checkOut).toBe('2026-12-04')
    expect(input.cost).toBe(13500)
    expect(input.placeId).toBeUndefined()
  })

  it('rounds the nightly price to cents when extending', () => {
    expect(extendedByOneNight(stay({ cost: 100, nights: 3 })).cost).toBe(133.33)
  })

  it('keeps a free stay free when extending', () => {
    expect(extendedByOneNight(stay({ cost: 0 })).cost).toBe(0)
  })

  it('offers 1 to 3 nights and every free night', () => {
    expect(quickCheckOuts('2026-12-01', '2026-12-06')).toEqual([
      { label: '1 night', checkOut: '2026-12-02' },
      { label: '2 nights', checkOut: '2026-12-03' },
      { label: '3 nights', checkOut: '2026-12-04' },
      { label: 'All 5 nights', checkOut: '2026-12-06' },
    ])
  })

  it('only offers choices that fit', () => {
    expect(quickCheckOuts('2026-12-01', '2026-12-03')).toEqual([
      { label: '1 night', checkOut: '2026-12-02' },
      { label: 'All 2 nights', checkOut: '2026-12-03' },
    ])
    expect(quickCheckOuts('2026-12-01', '2026-12-02')).toEqual([
      { label: '1 night', checkOut: '2026-12-02' },
    ])
    expect(quickCheckOuts('2026-12-03', '2026-12-02')).toEqual([])
  })
})

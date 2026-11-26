import { describe, expect, it } from 'vitest'
import type { Trip } from '@/types/trip'
import {
  countdownLabel,
  daysLabel,
  groupByMonth,
  groupTrips,
  progressLabel,
  searchTrips,
  todayIso,
  tripPhase,
} from '../trips'

function trip(id: string, startDate: string, endDate: string, days = 1): Trip {
  return { id, name: id, startDate, endDate, days }
}

describe('todayIso', () => {
  it('formats the local date', () => {
    expect(todayIso(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05')
  })
})

describe('tripPhase', () => {
  const today = '2026-12-10'

  it.each([
    ['2026-12-10', '2026-12-12', 'ongoing'],
    ['2026-12-08', '2026-12-10', 'ongoing'],
    ['2026-12-11', '2026-12-12', 'upcoming'],
    ['2026-12-01', '2026-12-09', 'past'],
  ])('a trip from %s to %s is %s', (start, end, phase) => {
    expect(tripPhase(trip('t', start, end), today)).toBe(phase)
  })
})

describe('groupTrips', () => {
  it('groups and orders trips: upcoming soonest first, past most recent first', () => {
    const trips = [
      trip('far', '2027-03-01', '2027-03-05'),
      trip('old', '2025-01-01', '2025-01-03'),
      trip('now', '2026-12-09', '2026-12-12'),
      trip('soon', '2026-12-20', '2026-12-22'),
      trip('recent', '2026-11-01', '2026-11-03'),
    ]

    const groups = groupTrips(trips, '2026-12-10')

    expect(groups.ongoing.map((t) => t.id)).toEqual(['now'])
    expect(groups.upcoming.map((t) => t.id)).toEqual(['soon', 'far'])
    expect(groups.past.map((t) => t.id)).toEqual(['recent', 'old'])
  })
})

describe('progressLabel', () => {
  it('shows which day of the trip today is', () => {
    expect(progressLabel(trip('t', '2026-12-09', '2026-12-13', 5), '2026-12-10')).toBe('Day 2 of 5')
  })
})

describe('countdownLabel', () => {
  const today = '2026-12-10'

  it.each([
    ['2026-12-11', 'Tomorrow'],
    ['2026-12-15', 'In 5 days'],
    ['2026-12-24', 'In 2 weeks'],
    ['2027-03-10', 'In 3 months'],
  ])('a trip starting %s is "%s"', (start, label) => {
    expect(countdownLabel(trip('t', start, start), today)).toBe(label)
  })
})

describe('daysLabel', () => {
  it('uses the singular for one day', () => {
    expect(daysLabel(1)).toBe('1 day')
    expect(daysLabel(3)).toBe('3 days')
  })
})

describe('groupByMonth', () => {
  it('groups trips by the month they start, keeping their order', () => {
    const groups = groupByMonth([
      trip('a', '2026-12-01', '2026-12-03'),
      trip('b', '2026-12-20', '2027-01-02'),
      trip('c', '2027-02-03', '2027-02-05'),
    ])

    expect(groups.map((group) => [group.label, group.trips.map((t) => t.id)])).toEqual([
      ['Dec 2026', ['a', 'b']],
      ['Feb 2027', ['c']],
    ])
  })
})

describe('searchTrips', () => {
  const kyoto: Trip = {
    ...trip('Autumn leaves', '2026-11-24', '2026-11-28'),
    destination: {
      placeId: 'kyoto',
      name: 'Kyoto',
      address: 'Japan',
      latitude: 0,
      longitude: 0,
      googleMapsUri: 'https://maps.google.com',
    },
  }
  const seoul = trip('Seoul food', '2027-02-03', '2027-02-05')

  it('matches the name or the destination, ignoring case and word order', () => {
    expect(searchTrips([kyoto, seoul], 'kyoto')).toEqual([kyoto])
    expect(searchTrips([kyoto, seoul], 'japan autumn')).toEqual([kyoto])
    expect(searchTrips([kyoto, seoul], 'FOOD')).toEqual([seoul])
    expect(searchTrips([kyoto, seoul], 'paris')).toEqual([])
  })

  it('returns everything for an empty search', () => {
    expect(searchTrips([kyoto, seoul], '  ')).toEqual([kyoto, seoul])
  })
})

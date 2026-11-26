import { describe, expect, it } from 'vitest'
import type { Trip } from '@/types/trip'
import {
  COVERS,
  countryOf,
  coverFor,
  coverStyle,
  flagFor,
  greeting,
  tripsSummary,
} from '../tripTheme'

function trip(name: string, address?: string, placeName = 'Somewhere'): Trip {
  return {
    id: name,
    name,
    startDate: '2026-12-01',
    endDate: '2026-12-02',
    days: 2,
    destination: address
      ? {
          placeId: `${name}-place`,
          name: placeName,
          address,
          latitude: 0,
          longitude: 0,
          googleMapsUri: 'https://maps.google.com',
        }
      : null,
  }
}

describe('coverFor', () => {
  it('always gives the same trip the same cover', () => {
    expect(coverFor(trip('Tokyo'))).toEqual(coverFor(trip('Tokyo')))
    expect(COVERS).toContainEqual(coverFor(trip('Tokyo')))
  })

  it('spreads trips across covers', () => {
    const names = ['Tokyo', 'Kyoto', 'Seoul', 'Taipei', 'Bangkok', 'Osaka', 'Paris', 'Rome']
    const covers = new Set(names.map((name) => coverFor(trip(name)).from))

    expect(covers.size).toBeGreaterThan(2)
  })

  it('exposes the cover as css variables', () => {
    expect(coverStyle({ from: '#111', to: '#222' })).toEqual({
      '--cover-from': '#111',
      '--cover-to': '#222',
    })
  })
})

describe('flags', () => {
  it.each([
    ['Japan', '🇯🇵'],
    ['Shibuya City, Tokyo, Japan', '🇯🇵'],
    ['日本東京都', null],
    ['台灣', '🇹🇼'],
    ['South Korea', '🇰🇷'],
    ['Atlantis', null],
  ])('reads %s as %s', (address, flag) => {
    expect(flagFor(trip('Trip', address))).toBe(flag)
  })

  it('has no flag without a destination', () => {
    expect(countryOf(trip('Trip'))).toBeNull()
    expect(flagFor(trip('Trip'))).toBeNull()
  })
})

describe('greeting', () => {
  it.each([
    [7, 'Good morning'],
    [13, 'Good afternoon'],
    [20, 'Good evening'],
    [2, 'Good evening'],
  ])('at %d:00 says %s', (hour, text) => {
    expect(greeting(new Date(2026, 10, 26, hour))).toBe(text)
  })
})

describe('tripsSummary', () => {
  const one = [trip('A')]

  it('counts trips happening now and coming up', () => {
    expect(tripsSummary({ ongoing: one, upcoming: [...one, ...one], past: [] })).toBe(
      '1 happening now · 2 coming up',
    )
    expect(tripsSummary({ ongoing: [], upcoming: one, past: one })).toBe('1 coming up')
  })

  it('invites to plan when nothing is ahead', () => {
    expect(tripsSummary({ ongoing: [], upcoming: [], past: one })).toBe(
      'Time to plan the next adventure.',
    )
    expect(tripsSummary({ ongoing: [], upcoming: [], past: [] })).toBe('Where to next?')
  })
})

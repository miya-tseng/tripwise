import { describe, expect, it } from 'vitest'
import type { Activity } from '@/types/activity'
import type { Place } from '@/types/place'
import { dayRouteLink, directionsLink, placeLink, placesOf, previousPlaceOf } from '../mapsLinks'

function place(id: string, latitude: number, longitude: number): Place {
  return {
    placeId: id,
    name: id,
    address: `${id} address`,
    latitude,
    longitude,
    googleMapsUri: `https://maps.google.com/?cid=${id}`,
  }
}

function activity(id: string, activityPlace: Place | null): Activity {
  return {
    id,
    name: id,
    date: '2026-12-01',
    startTime: '10:00',
    endTime: '11:00',
    cost: 0,
    currency: 'TWD',
    costInTwd: 0,
    place: activityPlace,
  }
}

const sensoji = place('senso-ji', 35.7148, 139.7967)
const skytree = place('skytree', 35.7101, 139.8107)
const ueno = place('ueno', 35.7148, 139.774)
const shibuya = place('shibuya', 35.6585, 139.7022)

function params(url: string): URLSearchParams {
  return new URL(url).searchParams
}

describe('placeLink', () => {
  it('opens the Google Maps page of the place', () => {
    expect(placeLink(sensoji)).toBe('https://maps.google.com/?cid=senso-ji')
  })
})

describe('directionsLink', () => {
  it('goes from one place to the next with coordinates and place ids', () => {
    const url = directionsLink(sensoji, skytree)

    expect(url.startsWith('https://www.google.com/maps/dir/?')).toBe(true)
    expect(Object.fromEntries(params(url))).toEqual({
      api: '1',
      origin: '35.7148,139.7967',
      origin_place_id: 'senso-ji',
      destination: '35.7101,139.8107',
      destination_place_id: 'skytree',
    })
  })

  it('encodes unusual place ids', () => {
    const url = directionsLink(place('a&b=c', 1, 2), skytree)

    expect(params(url).get('origin_place_id')).toBe('a&b=c')
  })
})

describe('dayRouteLink', () => {
  it('needs at least two places', () => {
    expect(dayRouteLink([])).toBeNull()
    expect(dayRouteLink([sensoji])).toBeNull()
  })

  it('goes from the first to the last place without waypoints for two places', () => {
    const query = params(dayRouteLink([sensoji, shibuya])!)

    expect(query.get('origin_place_id')).toBe('senso-ji')
    expect(query.get('destination_place_id')).toBe('shibuya')
    expect(query.has('waypoints')).toBe(false)
  })

  it('passes the places in between as waypoints in order', () => {
    const query = params(dayRouteLink([sensoji, skytree, ueno, shibuya])!)

    expect(query.get('waypoints')).toBe('35.7101,139.8107|35.7148,139.774')
    expect(query.get('waypoint_place_ids')).toBe('skytree|ueno')
  })

  it('limits the route to nine waypoints', () => {
    const many = Array.from({ length: 14 }, (_, index) => place(`p${index}`, index, index))

    const query = params(dayRouteLink(many)!)

    expect(query.get('waypoint_place_ids')!.split('|')).toHaveLength(9)
    expect(query.get('destination_place_id')).toBe('p13')
  })
})

describe('placesOf and previousPlaceOf', () => {
  const activities = [activity('a', sensoji), activity('free time', null), activity('b', skytree)]

  it('lists the places of activities in order', () => {
    expect(placesOf(activities)).toEqual([sensoji, skytree])
  })

  it('finds the previous place skipping activities without a place', () => {
    expect(previousPlaceOf(activities, 2)).toEqual(sensoji)
    expect(previousPlaceOf(activities, 0)).toBeNull()
  })
})

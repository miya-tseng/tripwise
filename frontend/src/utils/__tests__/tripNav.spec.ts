import { describe, expect, it } from 'vitest'
import { sectionOfRoute, tripRoute } from '../tripNav'

describe('trip navigation', () => {
  it('knows which trip section a route belongs to', () => {
    expect(sectionOfRoute('trip-schedule')).toBe('itinerary')
    expect(sectionOfRoute('trip-checklist')).toBe('checklist')
    expect(sectionOfRoute('home')).toBeNull()
    expect(sectionOfRoute(undefined)).toBeNull()
  })

  it('builds the route of a section', () => {
    expect(tripRoute('overview', 'tokyo-id')).toEqual({
      name: 'trip-overview',
      params: { id: 'tokyo-id' },
    })
  })
})

import { describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import TripRow from '../TripRow.vue'
import type { Trip } from '@/types/trip'

const seoul: Trip = {
  id: 'seoul-id',
  name: 'Seoul food',
  startDate: '2026-12-15',
  endDate: '2026-12-17',
  days: 3,
  destination: {
    placeId: 'seoul',
    name: 'Seoul',
    address: 'South Korea',
    latitude: 0,
    longitude: 0,
    googleMapsUri: 'https://maps.google.com',
  },
}

function mountRow(today: string) {
  return mount(TripRow, {
    props: { trip: seoul, today },
    global: { stubs: { RouterLink: RouterLinkStub } },
    attachTo: document.body,
  })
}

describe('TripRow', () => {
  it('shows the flag, name, dates, destination and a countdown', () => {
    const row = mountRow('2026-12-10')

    expect(row.text()).toContain('🇰🇷')
    expect(row.find('[data-testid="trip-row-link"]').text()).toBe('Seoul food')
    expect(row.text()).toContain('Dec 15 – 17, 2026 · Seoul')
    expect(row.find('[data-testid="trip-row-status"]').text()).toBe('In 5 days')
    expect(row.findComponent(RouterLinkStub).props('to')).toEqual({
      name: 'trip-schedule',
      params: { id: 'seoul-id' },
    })
    row.unmount()
  })

  it('shows progress while the trip is under way and the length once it is over', () => {
    const during = mountRow('2026-12-16')
    expect(during.find('[data-testid="trip-row-status"]').text()).toBe('Day 2 of 3')
    during.unmount()

    const after = mountRow('2027-01-01')
    expect(after.find('[data-testid="trip-row-status"]').text()).toBe('3 days')
    expect(after.find('[data-testid="trip-row"]').classes()).toContain('past')
    after.unmount()
  })

  it('asks to delete from its menu', async () => {
    const row = mountRow('2026-12-10')

    await row.find('[data-testid="trip-menu"]').trigger('click')
    await row.find('[data-testid="delete-button"]').trigger('click')

    expect(row.emitted('delete')).toEqual([[seoul]])
    row.unmount()
  })
})

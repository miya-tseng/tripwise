import { afterEach, describe, expect, it } from 'vitest'
import { mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import TripCard from '../TripCard.vue'
import type { Trip } from '@/types/trip'
import type { TripPhase } from '@/utils/trips'

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo with friends',
  startDate: '2026-12-08',
  endDate: '2026-12-12',
  days: 5,
  destination: {
    placeId: 'city-tokyo',
    name: 'Tokyo',
    address: 'Japan',
    latitude: 35.68,
    longitude: 139.65,
    googleMapsUri: 'https://maps.google.com/?cid=1',
  },
}

let wrapper: VueWrapper | null = null

function mountCard(phase: TripPhase, today: string, trip: Trip = tokyo, deleting = false) {
  wrapper = mount(TripCard, {
    props: { trip, phase, today, deleting },
    global: { stubs: { RouterLink: RouterLinkStub } },
    attachTo: document.body,
  })
  return wrapper
}

describe('TripCard', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('links to the trip and shows the destination and dates', () => {
    const card = mountCard('upcoming', '2026-12-01')

    expect(card.findComponent(RouterLinkStub).props('to')).toEqual({
      name: 'trip-schedule',
      params: { id: 'tokyo-id' },
    })
    expect(card.find('[data-testid="trip-link"]').text()).toBe('Tokyo with friends')
    expect(card.find('[data-testid="trip-destination"]').text()).toBe('Tokyo, Japan')
    expect(card.text()).toContain('Dec 8 – 12, 2026')
  })

  it('hides the destination when the trip has none', () => {
    const card = mountCard('upcoming', '2026-12-01', { ...tokyo, destination: null })

    expect(card.find('[data-testid="trip-destination"]').exists()).toBe(false)
  })

  it('shows progress and opens today for an ongoing trip', () => {
    const card = mountCard('ongoing', '2026-12-10')

    expect(card.find('[data-testid="trip-progress"]').text()).toBe('Day 3 of 5')
    expect(card.find('[role="progressbar"]').attributes('aria-valuenow')).toBe('3')
    const openToday = card
      .findAllComponents(RouterLinkStub)
      .find((link) => link.attributes('data-testid') === 'open-today')
    expect(openToday?.props('to')).toEqual({
      name: 'trip-schedule',
      params: { id: 'tokyo-id' },
      query: { day: '3' },
    })
  })

  it('shows a countdown for an upcoming trip', () => {
    const card = mountCard('upcoming', '2026-12-03')

    expect(card.find('[data-testid="trip-countdown"]').text()).toBe('In 5 days')
    expect(card.find('[data-testid="open-today"]').exists()).toBe(false)
  })

  it('shows only the length for a past trip', () => {
    const card = mountCard('past', '2027-01-01')

    expect(card.text()).toContain('5 days')
    expect(card.find('[data-testid="trip-countdown"]').exists()).toBe(false)
    expect(card.find('[data-testid="trip-progress"]').exists()).toBe(false)
  })

  it('keeps delete inside the actions menu', async () => {
    const card = mountCard('upcoming', '2026-12-01')

    expect(card.find('[data-testid="delete-button"]').exists()).toBe(false)

    await card.find('[data-testid="trip-menu"]').trigger('click')
    expect(card.find('[data-testid="trip-menu"]').attributes('aria-expanded')).toBe('true')

    await card.find('[data-testid="delete-button"]').trigger('click')

    expect(card.emitted('delete')).toEqual([[tokyo]])
    expect(card.find('[role="menu"]').exists()).toBe(false)
  })

  it('closes the menu when clicking elsewhere', async () => {
    const card = mountCard('upcoming', '2026-12-01')
    await card.find('[data-testid="trip-menu"]').trigger('click')

    document.body.dispatchEvent(new Event('pointerdown', { bubbles: true }))
    await card.vm.$nextTick()

    expect(card.find('[role="menu"]').exists()).toBe(false)
  })

  it('disables the menu while the trip is being deleted', () => {
    const card = mountCard('upcoming', '2026-12-01', tokyo, true)

    expect(card.find('[data-testid="trip-menu"]').attributes('disabled')).toBeDefined()
  })
})

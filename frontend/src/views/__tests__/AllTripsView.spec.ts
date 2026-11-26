import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { deleteTrip, fetchTrips } from '@/api/trips'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { Trip } from '@/types/trip'
import AllTripsView from '../AllTripsView.vue'

vi.mock('@/api/trips')

function trip(id: string, startDate: string, endDate: string): Trip {
  return { id, name: id, startDate, endDate, days: 2 }
}

const trips = [
  trip('Kyoto', '2026-12-09', '2026-12-12'),
  trip('Tokyo', '2026-12-20', '2026-12-22'),
  trip('Seoul', '2027-02-03', '2027-02-05'),
  trip('Taipei', '2026-10-01', '2026-10-03'),
]

let wrapper: VueWrapper | null = null

async function mountView() {
  vi.mocked(fetchTrips).mockResolvedValue(trips)
  wrapper = mount(AllTripsView, {
    props: { today: '2026-12-10' },
    global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
    attachTo: document.body,
  })
  await flushPromises()
  return wrapper
}

function months(view: VueWrapper) {
  return view
    .findAll('[data-testid="trip-month"]')
    .map((month) => [
      month.find('h2').text(),
      month.findAll('[data-testid="trip-row-link"]').map((link) => link.text()),
    ])
}

describe('AllTripsView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('lists current and upcoming trips by month', async () => {
    const view = await mountView()

    expect(view.find('h1').text()).toBe('All trips')
    expect(view.find('[data-testid="tab-upcoming"]').text()).toBe('Upcoming 3')
    expect(view.find('[data-testid="tab-past"]').text()).toBe('Past 1')
    expect(months(view)).toEqual([
      ['Dec 2026', ['Kyoto', 'Tokyo']],
      ['Feb 2027', ['Seoul']],
    ])
  })

  it('switches to past trips', async () => {
    const view = await mountView()

    await view.find('[data-testid="tab-past"]').trigger('click')

    expect(months(view)).toEqual([['Oct 2026', ['Taipei']]])
  })

  it('searches within the tab', async () => {
    const view = await mountView()

    await view.find('[data-testid="trip-search"]').setValue('seo')
    expect(months(view)).toEqual([['Feb 2027', ['Seoul']]])

    await view.find('[data-testid="trip-search"]').setValue('paris')
    expect(view.find('[data-testid="no-trips"]').text()).toBe('No trips match "paris".')
  })

  it('deletes a trip after confirming', async () => {
    vi.mocked(deleteTrip).mockResolvedValue()
    const view = await mountView()

    await view.find('[data-testid="trip-menu"]').trigger('click')
    await view.find('[data-testid="delete-button"]').trigger('click')
    view.findComponent(ConfirmDialog).vm.$emit('confirm')
    await flushPromises()

    expect(deleteTrip).toHaveBeenCalledWith('Kyoto')
    expect(months(view)[0]).toEqual(['Dec 2026', ['Tokyo']])
  })
})

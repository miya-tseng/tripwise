import { describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import TripList from '../TripList.vue'
import type { Trip } from '@/types/trip'

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-20',
  endDate: '2026-12-22',
  days: 3,
}
const osaka: Trip = {
  id: 'osaka-id',
  name: 'Osaka',
  startDate: '2027-01-05',
  endDate: '2027-01-06',
  days: 2,
}

const global = { stubs: { RouterLink: RouterLinkStub } }

describe('TripList', () => {
  it('renders a card per trip in the given phase', () => {
    const wrapper = mount(TripList, {
      props: { trips: [tokyo, osaka], phase: 'upcoming', today: '2026-12-10' },
      global,
    })

    const items = wrapper.findAll('[data-testid="trip-item"]')
    expect(items.map((item) => item.attributes('data-phase'))).toEqual(['upcoming', 'upcoming'])
    expect(wrapper.findAll('[data-testid="trip-link"]').map((link) => link.text())).toEqual([
      'Tokyo',
      'Osaka',
    ])
  })

  it('passes the delete request up with the trip', async () => {
    const wrapper = mount(TripList, {
      props: { trips: [tokyo], phase: 'upcoming', today: '2026-12-10' },
      global,
      attachTo: document.body,
    })

    await wrapper.find('[data-testid="trip-menu"]').trigger('click')
    await wrapper.find('[data-testid="delete-button"]').trigger('click')

    expect(wrapper.emitted('delete')).toEqual([[tokyo]])
    wrapper.unmount()
  })
})

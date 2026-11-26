import { describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import BottomNav from '../BottomNav.vue'

describe('BottomNav', () => {
  it('links home and to every trip section, marking the current one', () => {
    const wrapper = mount(BottomNav, {
      props: { tripId: 'tokyo-id', active: 'overview' },
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    const links = wrapper.findAllComponents(RouterLinkStub)

    expect(links.map((link) => link.text())).toEqual([
      '🏠Trips',
      '🗓Itinerary',
      '📊Overview',
      '✅Checklist',
    ])
    expect(links[0]!.props('to')).toBe('/')
    expect(links[3]!.props('to')).toEqual({ name: 'trip-checklist', params: { id: 'tokyo-id' } })
    expect(wrapper.find('[data-testid="nav-overview"]').attributes('aria-current')).toBe('page')
    expect(wrapper.find('[data-testid="nav-itinerary"]').attributes('aria-current')).toBeUndefined()
  })
})

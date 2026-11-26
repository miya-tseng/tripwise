import { describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import AppNav from '../AppNav.vue'

describe('AppNav', () => {
  it('links home, to a new trip and to all trips', () => {
    const wrapper = mount(AppNav, {
      props: { active: 'all-trips' },
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    const targets = wrapper.findAllComponents(RouterLinkStub).map((link) => link.props('to'))

    expect(targets).toEqual([
      { name: 'home' },
      { name: 'home', query: { new: '1' } },
      { name: 'all-trips' },
    ])
    expect(wrapper.find('[data-testid="nav-all-trips"]').attributes('aria-current')).toBe('page')
    expect(wrapper.find('[data-testid="nav-home"]').attributes('aria-current')).toBeUndefined()
  })
})

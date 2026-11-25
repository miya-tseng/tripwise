import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { fetchTrips } from '@/api/trips'
import HomeView from '../HomeView.vue'

vi.mock('@/api/trips')

function mountHomeView() {
  return mount(HomeView, {
    global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
  })
}

describe('HomeView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('renders the app title', async () => {
    vi.mocked(fetchTrips).mockResolvedValue([])

    const wrapper = mountHomeView()
    await flushPromises()

    expect(wrapper.find('h1').text()).toBe('TripWise')
  })

  it('loads and shows the trips', async () => {
    vi.mocked(fetchTrips).mockResolvedValue([
      { id: 'tokyo-id', name: 'Tokyo', startDate: '2026-12-01', endDate: '2026-12-05', days: 5 },
    ])

    const wrapper = mountHomeView()
    await flushPromises()

    expect(wrapper.findAll('[data-testid="trip-item"]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Tokyo')
  })

  it('shows the error when loading fails', async () => {
    vi.mocked(fetchTrips).mockRejectedValue(new Error('Network error'))

    const wrapper = mountHomeView()
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toContain('Network error')
  })

  it('hides the error when it is dismissed', async () => {
    vi.mocked(fetchTrips).mockRejectedValue(new Error('Network error'))
    const wrapper = mountHomeView()
    await flushPromises()

    await wrapper.find('[data-testid="dismiss-error"]').trigger('click')

    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })
})

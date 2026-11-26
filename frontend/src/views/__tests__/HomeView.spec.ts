import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createTrip, deleteTrip, fetchTrips } from '@/api/trips'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import NewTripDialog from '@/components/NewTripDialog.vue'
import type { Trip } from '@/types/trip'
import HomeView from '../HomeView.vue'

vi.mock('@/api/trips')

const today = '2026-12-10'

function trip(id: string, startDate: string, endDate: string): Trip {
  return { id, name: id, startDate, endDate, days: 1 }
}

const ongoing = trip('Kyoto', '2026-12-09', '2026-12-12')
const upcoming = trip('Tokyo', '2026-12-20', '2026-12-22')
const past = trip('Taipei', '2026-10-01', '2026-10-03')

let wrapper: VueWrapper | null = null

async function mountHomeView(trips: Trip[] = []) {
  vi.mocked(fetchTrips).mockResolvedValue(trips)
  wrapper = mount(HomeView, {
    props: { today },
    global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
    attachTo: document.body,
  })
  await flushPromises()
  return wrapper
}

function names(view: VueWrapper, section: string) {
  return view
    .find(`[data-testid="${section}"]`)
    .findAll('[data-testid="trip-link"]')
    .map((link) => link.text())
}

describe('HomeView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('shows the app bar, the date and a summary of the trips', async () => {
    const view = await mountHomeView([upcoming, ongoing])

    expect(view.find('[data-testid="app-bar"]').text()).toContain('TripWise')
    expect(view.find('h1').text()).toMatch(/^Good (morning|afternoon|evening)$/)
    expect(view.find('[data-testid="today-label"]').text()).toBe('Thu, Dec 10')
    expect(view.find('[data-testid="trips-summary"]').text()).toBe('1 happening now · 1 coming up')
  })

  it('shows an empty state that opens the new trip dialog', async () => {
    const view = await mountHomeView()

    expect(view.find('[data-testid="empty-state"]').text()).toBe('No trips yet.')
    await view.find('[data-testid="new-trip"]').trigger('click')

    expect(view.findComponent(NewTripDialog).props('open')).toBe(true)
  })

  it('shows the trip happening now and what is coming up', async () => {
    const view = await mountHomeView([upcoming, past, ongoing])

    expect(names(view, 'ongoing-section')).toEqual(['Kyoto'])
    expect(names(view, 'upcoming-section')).toEqual(['Tokyo'])
    expect(view.find('[data-testid="past-section"]').exists()).toBe(false)
  })

  it('keeps the home page short: three trips and a link to the rest', async () => {
    const later = [1, 2, 3, 4, 5].map((n) => trip(`Later ${n}`, `2027-0${n}-01`, `2027-0${n}-02`))
    const view = await mountHomeView([...later, past])

    expect(names(view, 'upcoming-section')).toEqual(['Later 1', 'Later 2', 'Later 3'])
    expect(view.find('[data-testid="see-all-upcoming"]').text()).toBe('See all 5')
    const allTrips = view.find('[data-testid="all-trips-link"]')
    expect(allTrips.text()).toContain('6 trips · 1 past')
    expect(
      view
        .findAllComponents(RouterLinkStub)
        .find((link) => link.attributes('data-testid') === 'all-trips-link')!
        .props('to'),
    ).toEqual({ name: 'all-trips' })
  })

  it('opens the new trip dialog when asked from the bottom menu', async () => {
    vi.mocked(fetchTrips).mockResolvedValue([upcoming])
    wrapper = mount(HomeView, {
      props: { today, create: '1' },
      global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()

    expect(wrapper.findComponent(NewTripDialog).props('open')).toBe(true)
  })

  it('hides sections that have nothing to show', async () => {
    const view = await mountHomeView([upcoming])

    expect(view.find('[data-testid="ongoing-section"]').exists()).toBe(false)
    expect(view.find('[data-testid="past-section"]').exists()).toBe(false)
  })

  it('offers to plan the next trip when nothing is upcoming', async () => {
    const view = await mountHomeView([past])

    await view.find('[data-testid="add-upcoming"]').trigger('click')

    expect(view.findComponent(NewTripDialog).props('open')).toBe(true)
  })

  it('adds the created trip to its group and closes the dialog', async () => {
    const created = trip('Osaka', '2027-01-05', '2027-01-06')
    vi.mocked(createTrip).mockResolvedValue(created)
    const view = await mountHomeView([upcoming])

    await view.find('[data-testid="new-trip"]').trigger('click')
    const accepted = await view.findComponent(NewTripDialog).props('submit')({
      name: 'Osaka',
      startDate: '2027-01-05',
      endDate: '2027-01-06',
    })
    view.findComponent(NewTripDialog).vm.$emit('close')
    await flushPromises()

    expect(accepted).toBe(true)
    expect(names(view, 'upcoming-section')).toEqual(['Tokyo', 'Osaka'])
    expect(view.findComponent(NewTripDialog).props('open')).toBe(false)
  })

  it('shows a create error inside the dialog instead of the page', async () => {
    vi.mocked(createTrip).mockRejectedValue(new Error('Place not found.'))
    const view = await mountHomeView([upcoming])

    await view.find('[data-testid="new-trip"]').trigger('click')
    await view.findComponent(NewTripDialog).props('submit')({
      name: 'Osaka',
      startDate: '2027-01-05',
      endDate: '2027-01-06',
    })
    await flushPromises()

    expect(view.findComponent(NewTripDialog).props('error')).toBe('Place not found.')
    expect(view.find('[data-testid="dismiss-error"]').exists()).toBe(false)
  })

  it('deletes a trip after confirming', async () => {
    vi.mocked(deleteTrip).mockResolvedValue()
    const view = await mountHomeView([upcoming])

    await view.find('[data-testid="trip-menu"]').trigger('click')
    await view.find('[data-testid="delete-button"]').trigger('click')
    const confirm = view.findComponent(ConfirmDialog)
    expect(confirm.props('message')).toContain('"Tokyo"')

    confirm.vm.$emit('confirm')
    await flushPromises()

    expect(deleteTrip).toHaveBeenCalledWith('Tokyo')
    expect(view.findAll('[data-testid="trip-item"]')).toHaveLength(0)
  })

  it('does not delete when cancelled', async () => {
    const view = await mountHomeView([upcoming])

    await view.find('[data-testid="trip-menu"]').trigger('click')
    await view.find('[data-testid="delete-button"]').trigger('click')
    view.findComponent(ConfirmDialog).vm.$emit('cancel')
    await flushPromises()

    expect(deleteTrip).not.toHaveBeenCalled()
    expect(view.findComponent(ConfirmDialog).props('open')).toBe(false)
  })

  it('shows the error when loading fails and hides it when dismissed', async () => {
    vi.mocked(fetchTrips).mockRejectedValue(new Error('Network error'))
    wrapper = mount(HomeView, {
      props: { today },
      global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toContain('Network error')
    await wrapper.find('[data-testid="dismiss-error"]').trigger('click')
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })
})

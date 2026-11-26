import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DOMWrapper, flushPromises, mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import { routerKey, type Router } from 'vue-router'
import { createPinia } from 'pinia'
import {
  addActivity,
  fetchSchedule,
  fetchSuggestions,
  moveActivity,
  updateActivity,
} from '@/api/activities'
import { fetchExchangeRates } from '@/api/exchangeRates'
import { fetchTrip, updateTrip } from '@/api/trips'
import { updateStay } from '@/api/stays'
import StayDialog from '@/components/StayDialog.vue'
import type { Activity } from '@/types/activity'
import ActivityForm from '@/components/ActivityForm.vue'
import TripEditDialog from '@/components/TripEditDialog.vue'
import ActivityDialog from '@/components/ActivityDialog.vue'
import BoardView from '@/components/BoardView.vue'
import ScheduleDays from '@/components/ScheduleDays.vue'
import SuggestionsPanel from '@/components/SuggestionsPanel.vue'
import { createMemoryStorage } from '@/test/memoryStorage'
import TableView from '@/components/TableView.vue'
import TripScheduleView from '../TripScheduleView.vue'

vi.mock('@/api/activities')
vi.mock('@/api/trips')
vi.mock('@/api/exchangeRates')
vi.mock('@/api/stays')

const shibuya: Activity = {
  id: 'shibuya-id',
  name: 'Shibuya',
  date: '2026-12-01',
  startTime: '15:00:00',
  endTime: '16:00:00',
  cost: 2200,
  currency: 'TWD',
  costInTwd: 2200,
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

function mockLoadedTrip() {
  vi.mocked(fetchTrip).mockResolvedValue({
    id: 'tokyo-id',
    name: 'Tokyo',
    startDate: '2026-12-01',
    endDate: '2026-12-02',
    days: 2,
  })
  vi.mocked(fetchSchedule).mockResolvedValue({
    tripId: 'tokyo-id',
    ratesAvailable: true,
    ratesUpdatedAt: '2026-01-01T00:00:00Z',
    days: [
      { date: '2026-12-01', activities: [shibuya] },
      { date: '2026-12-02', activities: [] },
    ],
  })
}

async function mountView() {
  const wrapper = mount(TripScheduleView, {
    props: { id: 'tokyo-id' },
    global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
  })
  await flushPromises()
  return wrapper
}

async function pickTime(wrapper: VueWrapper, name: 'startTime' | 'endTime', time: string) {
  const picker = wrapper.find(`input[name="${name}"]`).element.parentElement!
  const scope = new DOMWrapper(picker)
  const [hour, minute] = time.split(':')
  await scope.find('[data-testid="time-trigger"]').trigger('click')
  const hourOption = scope
    .findAll('[data-testid="hour-option"]')
    .find((option) => option.text() === hour)
  await hourOption!.trigger('click')
  const minuteOption = scope
    .findAll('[data-testid="minute-option"]')
    .find((option) => option.text() === minute)
  await minuteOption!.trigger('click')
}

async function submitActivity(
  wrapper: Awaited<ReturnType<typeof mountView>>,
  values: { name: string; startTime: string; endTime: string; cost?: string },
) {
  await wrapper.find('[data-testid="add-activity"]').trigger('click')
  await wrapper.find('[data-testid="use-custom-activity"]').trigger('click')
  await wrapper.find('input[name="name"]').setValue(values.name)
  await pickTime(wrapper, 'startTime', values.startTime)
  await pickTime(wrapper, 'endTime', values.endTime)
  if (values.cost !== undefined) {
    await wrapper.find('input[name="cost"]').setValue(values.cost)
  }
  await wrapper.find('form').trigger('submit')
  await flushPromises()
}

function dialogTitle(wrapper: Awaited<ReturnType<typeof mountView>>): string | null {
  const dialog = wrapper.find('[data-testid="activity-dialog"]')
  return dialog.exists() ? dialog.find('h2').text() : null
}

function activityNames(wrapper: Awaited<ReturnType<typeof mountView>>): string[] {
  return wrapper.findAll('[data-testid="activity-name"]').map((item) => item.text())
}

describe('TripScheduleView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.stubGlobal('localStorage', createMemoryStorage())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the trip and its schedule', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    expect(wrapper.find('h1').text()).toBe('Tokyo')
    expect(wrapper.find('[data-testid="trip-dates"]').text()).toBe('Dec 1 – 2, 2026')
    expect(wrapper.find('[data-testid="trip-days"]').text()).toBe('2')
    expect(wrapper.find('[data-testid="activity-count"]').text()).toBe('1')
    expect(wrapper.findAll('[data-testid="schedule-day"]')).toHaveLength(2)
    expect(activityNames(wrapper)).toEqual(['Shibuya'])
  })

  it('links back to all trips', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    expect(wrapper.findComponent(RouterLinkStub).props('to')).toBe('/')
  })

  it('shows an error when the trip cannot be loaded', async () => {
    vi.mocked(fetchTrip).mockRejectedValue(new Error('Request failed with status 404'))
    vi.mocked(fetchSchedule).mockRejectedValue(new Error('Request failed with status 404'))

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="load-error"]').text()).toBe('Request failed with status 404')
    expect(wrapper.find('form').exists()).toBe(false)
  })

  it('adds an activity in start time order', async () => {
    mockLoadedTrip()
    vi.mocked(addActivity).mockResolvedValue({
      id: 'senso-ji-id',
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00:00',
      endTime: '11:00:00',
      cost: 500.25,
      currency: 'TWD',
      costInTwd: 500.25,
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    const wrapper = await mountView()

    await submitActivity(wrapper, {
      name: 'Senso-ji',
      startTime: '10:00',
      endTime: '11:00',
      cost: '500.25',
    })

    expect(addActivity).toHaveBeenCalledWith('tokyo-id', {
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00',
      endTime: '11:00',
      cost: 500.25,
      currency: 'TWD',
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    expect(activityNames(wrapper)).toEqual(['Senso-ji', 'Shibuya'])
  })

  it('shows the trip budget and day subtotals', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="trip-total"]').text()).toBe('NT$2,200.00')
    const dayTotals = wrapper.findAll('[data-testid="day-total"]').map((item) => item.text())
    expect(dayTotals).toEqual(['NT$2,200.00', 'NT$0.00'])
  })

  it('updates the budget after adding an activity with a cost', async () => {
    mockLoadedTrip()
    vi.mocked(addActivity).mockResolvedValue({
      id: 'senso-ji-id',
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00:00',
      endTime: '11:00:00',
      cost: 500.25,
      currency: 'TWD',
      costInTwd: 500.25,
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    const wrapper = await mountView()

    await submitActivity(wrapper, {
      name: 'Senso-ji',
      startTime: '10:00',
      endTime: '11:00',
      cost: '500.25',
    })

    expect(wrapper.find('[data-testid="trip-total"]').text()).toBe('NT$2,700.25')
    expect(wrapper.find('[data-testid="day-total"]').text()).toBe('NT$2,700.25')
  })

  it('shows the overlap error from the server', async () => {
    mockLoadedTrip()
    const detail = 'The activity overlaps with "Shibuya" (15:00-16:00) on 2026-12-01.'
    vi.mocked(addActivity).mockRejectedValue(new Error(detail))
    const wrapper = await mountView()

    await submitActivity(wrapper, { name: 'Hachiko', startTime: '15:30', endTime: '16:30' })

    expect(wrapper.find('[data-testid="schedule-error"]').text()).toContain(detail)
    expect(activityNames(wrapper)).toEqual(['Shibuya'])
  })

  it('edits an activity and leaves edit mode after saving', async () => {
    mockLoadedTrip()
    vi.mocked(updateActivity).mockResolvedValue({ ...shibuya, name: 'Shibuya Sky' })
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-activity"]').trigger('click')
    expect(dialogTitle(wrapper)).toBe('Edit "Shibuya"')
    expect(wrapper.findComponent(ActivityForm).props('activity')).toEqual(shibuya)

    await wrapper.find('input[name="name"]').setValue('Shibuya Sky')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(updateActivity).toHaveBeenCalledWith('tokyo-id', 'shibuya-id', {
      name: 'Shibuya Sky',
      date: '2026-12-01',
      startTime: '15:00',
      endTime: '16:00',
      cost: 2200,
      currency: 'TWD',
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    expect(activityNames(wrapper)).toEqual(['Shibuya Sky'])
    expect(dialogTitle(wrapper)).toBeNull()
  })

  it('stays in edit mode when saving fails', async () => {
    mockLoadedTrip()
    vi.mocked(updateActivity).mockRejectedValue(new Error('Overlap'))
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-activity"]').trigger('click')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(dialogTitle(wrapper)).toBe('Edit "Shibuya"')
    expect(
      wrapper.find('[data-testid="activity-dialog"] [data-testid="schedule-error"]').text(),
    ).toContain('Overlap')
  })

  it('cancels editing', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-activity"]').trigger('click')
    await wrapper.find('[data-testid="cancel-edit"]').trigger('click')

    expect(dialogTitle(wrapper)).toBeNull()
  })

  it('edits the trip from the hero and shows the new details', async () => {
    mockLoadedTrip()
    vi.mocked(updateTrip).mockResolvedValue({
      id: 'tokyo-id',
      name: 'Osaka',
      startDate: '2026-12-01',
      endDate: '2026-12-03',
      days: 3,
    })
    const wrapper = await mountView()
    vi.mocked(fetchSchedule).mockResolvedValue({
      tripId: 'tokyo-id',
      ratesAvailable: true,
      ratesUpdatedAt: '2026-01-01T00:00:00Z',
      days: [
        { date: '2026-12-01', activities: [shibuya] },
        { date: '2026-12-02', activities: [] },
        { date: '2026-12-03', activities: [] },
      ],
    })

    await wrapper.find('[data-testid="edit-trip"]').trigger('click')
    const dialog = wrapper.findComponent(TripEditDialog)
    expect(dialog.props('open')).toBe(true)

    await dialog.props('submit')({ name: 'Osaka', startDate: '2026-12-01', endDate: '2026-12-03' })
    dialog.vm.$emit('close')
    await flushPromises()

    expect(wrapper.find('h1').text()).toBe('Osaka')
    expect(wrapper.find('[data-testid="trip-days"]').text()).toBe('3')
    expect(wrapper.findAll('[data-testid="schedule-day"]')).toHaveLength(3)
    expect(wrapper.findComponent(TripEditDialog).props('open')).toBe(false)
  })

  it('passes the conflict error to the edit dialog', async () => {
    mockLoadedTrip()
    vi.mocked(updateTrip).mockRejectedValue(new Error('1 activity falls outside the new dates.'))
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-trip"]').trigger('click')
    const saved = await wrapper.findComponent(TripEditDialog).props('submit')({
      name: 'Tokyo',
      startDate: '2026-12-02',
      endDate: '2026-12-02',
    })
    await flushPromises()

    expect(saved).toBe(false)
    expect(wrapper.findComponent(TripEditDialog).props('error')).toBe(
      '1 activity falls outside the new dates.',
    )
    expect(wrapper.find('h1').text()).toBe('Tokyo')
  })

  it('notes where the exchange rates come from', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="rates-note"]').text()).toBe(
      'Totals in TWD · Rates by Exchange Rate API, updated Jan 1, 2026',
    )
  })

  it('warns and shows totals per currency when rates are unavailable', async () => {
    mockLoadedTrip()
    vi.mocked(fetchSchedule).mockResolvedValue({
      tripId: 'tokyo-id',
      ratesAvailable: false,
      ratesUpdatedAt: null,
      days: [
        {
          date: '2026-12-01',
          activities: [{ ...shibuya, cost: 3000, currency: 'JPY', costInTwd: null }],
        },
        { date: '2026-12-02', activities: [] },
      ],
    })

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="rates-note"]').text()).toBe(
      'Exchange rates are unavailable right now, so totals are shown per currency.',
    )
    expect(wrapper.find('[data-testid="trip-total"]').text()).toBe('¥3,000')
  })

  it('remembers the currency of the last added cost for this trip', async () => {
    vi.stubGlobal('localStorage', createMemoryStorage())
    mockLoadedTrip()
    vi.mocked(fetchExchangeRates).mockResolvedValue({
      baseCurrency: 'TWD',
      available: true,
      updatedAt: '2026-01-01T00:00:00Z',
      currencies: ['JPY', 'TWD'],
      unitsPerBaseCurrency: { TWD: 1, JPY: 4.8 },
    })
    vi.mocked(addActivity).mockResolvedValue({
      id: 'senso-ji-id',
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00:00',
      endTime: '11:00:00',
      cost: 500,
      currency: 'JPY',
      costInTwd: 104.17,
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    const wrapper = await mountView()
    await wrapper.find('[data-testid="add-activity"]').trigger('click')

    await wrapper.findComponent(ActivityForm).props('submit')({
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00',
      endTime: '11:00',
      cost: 500,
      currency: 'JPY',
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    })
    await flushPromises()

    expect(localStorage.getItem('tripwise.currency.tokyo-id')).toBe('JPY')
    expect(wrapper.findComponent(ActivityForm).exists()).toBe(false)
    await wrapper.find('[data-testid="add-activity"]').trigger('click')
    expect(wrapper.findComponent(ActivityForm).props('defaultCurrency')).toBe('JPY')
    vi.unstubAllGlobals()
  })

  it('biases place search near the last place of the trip', async () => {
    mockLoadedTrip()
    vi.mocked(fetchSchedule).mockResolvedValue({
      tripId: 'tokyo-id',
      ratesAvailable: true,
      ratesUpdatedAt: '2026-01-01T00:00:00Z',
      days: [
        {
          date: '2026-12-01',
          activities: [
            {
              ...shibuya,
              place: {
                placeId: 'shibuya-id',
                name: 'Shibuya Sky',
                address: 'Shibuya, Tokyo',
                latitude: 35.658,
                longitude: 139.702,
                googleMapsUri: 'https://maps.google.com/?cid=3',
              },
            },
          ],
        },
        { date: '2026-12-02', activities: [] },
      ],
    })

    const wrapper = await mountView()
    await wrapper.find('[data-testid="add-activity"]').trigger('click')

    expect(wrapper.findComponent(ActivityForm).props('near')).toEqual({
      latitude: 35.658,
      longitude: 139.702,
    })
  })

  it('biases place search near the destination until the trip has places', async () => {
    mockLoadedTrip()
    vi.mocked(fetchTrip).mockResolvedValue({
      id: 'tokyo-id',
      name: 'Tokyo',
      startDate: '2026-12-01',
      endDate: '2026-12-02',
      days: 2,
      destination: {
        placeId: 'city-tokyo',
        name: 'Tokyo',
        address: 'Japan',
        latitude: 35.68,
        longitude: 139.65,
        googleMapsUri: 'https://maps.google.com/?cid=1',
      },
    })

    const wrapper = await mountView()
    await wrapper.find('[data-testid="add-activity"]').trigger('click')

    expect(wrapper.findComponent(ActivityForm).props('near')).toEqual({
      latitude: 35.68,
      longitude: 139.65,
    })
  })

  it('scrolls to the day given in the link', async () => {
    mockLoadedTrip()
    const scrollIntoView = vi.fn<(options?: ScrollIntoViewOptions) => void>()
    Element.prototype.scrollIntoView = scrollIntoView

    const wrapper = mount(TripScheduleView, {
      props: { id: 'tokyo-id', day: '2' },
      global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
      attachTo: document.body,
    })
    await flushPromises()

    expect(scrollIntoView).toHaveBeenCalledTimes(1)
    const scrolled = scrollIntoView.mock.contexts[0] as Element
    expect(scrolled).toBe(wrapper.findAll('[data-testid="schedule-day"]')[1]!.element)
    wrapper.unmount()
    Reflect.deleteProperty(Element.prototype, 'scrollIntoView')
  })

  it('scrolls to today and highlights it when the trip is under way', async () => {
    mockLoadedTrip()
    const scrollIntoView = vi.fn<(options?: ScrollIntoViewOptions) => void>()
    Element.prototype.scrollIntoView = scrollIntoView

    const wrapper = mount(TripScheduleView, {
      props: { id: 'tokyo-id', today: '2026-12-02' },
      global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
      attachTo: document.body,
    })
    await flushPromises()

    const today = wrapper.findAll('[data-testid="schedule-day"]')[1]!
    expect(today.classes()).toContain('is-today')
    expect(scrollIntoView.mock.contexts[0]).toBe(today.element)
    wrapper.unmount()
    Reflect.deleteProperty(Element.prototype, 'scrollIntoView')
  })

  it('does not scroll when today is the first day', async () => {
    mockLoadedTrip()
    const scrollIntoView = vi.fn<(options?: ScrollIntoViewOptions) => void>()
    Element.prototype.scrollIntoView = scrollIntoView

    const wrapper = mount(TripScheduleView, {
      props: { id: 'tokyo-id', today: '2026-12-01' },
      global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()

    expect(scrollIntoView).not.toHaveBeenCalled()
    wrapper.unmount()
    Reflect.deleteProperty(Element.prototype, 'scrollIntoView')
  })

  describe('suggestions', () => {
    const shibuyaWithPlace: Activity = {
      ...shibuya,
      place: {
        placeId: 'shibuya-id',
        name: 'Shibuya Sky',
        address: 'Shibuya, Tokyo',
        latitude: 35.658,
        longitude: 139.702,
        googleMapsUri: 'https://maps.google.com/?cid=3',
      },
    }
    const meiji = {
      place: {
        placeId: 'meiji-id',
        name: 'Meiji Jingu',
        address: 'Yoyogi, Tokyo',
        latitude: 35.676,
        longitude: 139.699,
        googleMapsUri: 'https://maps.google.com/?cid=4',
      },
      reason: 'Quiet shrine nearby',
      suggestedMinutes: 90,
    }

    function mockTripWithPlace() {
      mockLoadedTrip()
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: 'tokyo-id',
        ratesAvailable: true,
        ratesUpdatedAt: '2026-01-01T00:00:00Z',
        days: [
          { date: '2026-12-01', activities: [shibuyaWithPlace] },
          { date: '2026-12-02', activities: [] },
        ],
      })
      vi.mocked(fetchSuggestions).mockResolvedValue([meiji])
    }

    it('shows ideas for an activity', async () => {
      mockTripWithPlace()
      const wrapper = await mountView()

      await wrapper.find('[data-testid="suggest-activity"]').trigger('click')
      await flushPromises()

      const panel = wrapper.findComponent(SuggestionsPanel)
      expect(fetchSuggestions).toHaveBeenCalledWith('tokyo-id', 'shibuya-id')
      expect(panel.props('anchor')).toEqual(shibuyaWithPlace)
      expect(panel.props('suggestions')).toEqual([meiji])
    })

    it('fills the form with a suggestion right after the activity', async () => {
      mockTripWithPlace()
      const wrapper = await mountView()
      await wrapper.find('[data-testid="suggest-activity"]').trigger('click')
      await flushPromises()

      wrapper.findComponent(SuggestionsPanel).vm.$emit('add', meiji)
      await flushPromises()

      expect(wrapper.findComponent(ActivityForm).props('prefill')).toEqual({
        place: { placeId: 'meiji-id', name: 'Meiji Jingu', description: 'Yoyogi, Tokyo' },
        date: '2026-12-01',
        startTime: '16:00',
        endTime: '17:30',
      })
    })

    it('removes the suggestion after it is added to the trip', async () => {
      mockTripWithPlace()
      vi.mocked(addActivity).mockResolvedValue({
        ...shibuyaWithPlace,
        id: 'meiji-activity',
        name: 'Meiji Jingu',
        startTime: '16:00:00',
        endTime: '17:30:00',
        place: meiji.place,
      })
      const wrapper = await mountView()
      await wrapper.find('[data-testid="suggest-activity"]').trigger('click')
      await flushPromises()
      wrapper.findComponent(SuggestionsPanel).vm.$emit('add', meiji)
      await flushPromises()

      await wrapper.findComponent(ActivityForm).props('submit')({
        name: 'Meiji Jingu',
        placeId: 'meiji-id',
        placeSessionToken: 'token',
        date: '2026-12-01',
        startTime: '16:00',
        endTime: '17:30',
        cost: 0,
        currency: 'TWD',
        category: 'Other',
        bookingStatus: 'NotNeeded',
        notes: '',
        confirmationCode: '',
        link: null,
      })
      await flushPromises()

      expect(wrapper.findComponent(SuggestionsPanel).props('suggestions')).toEqual([])
      expect(wrapper.findComponent(ActivityDialog).props('open')).toBe(false)
    })

    it('closes the ideas panel', async () => {
      mockTripWithPlace()
      const wrapper = await mountView()
      await wrapper.find('[data-testid="suggest-activity"]').trigger('click')
      await flushPromises()

      wrapper.findComponent(SuggestionsPanel).vm.$emit('close')
      await flushPromises()

      expect(wrapper.findComponent(SuggestionsPanel).exists()).toBe(false)
    })
  })

  describe('board view', () => {
    it('switches between the timeline and the board', async () => {
      mockLoadedTrip()
      const wrapper = await mountView()
      expect(wrapper.findComponent(ScheduleDays).exists()).toBe(true)

      await wrapper.find('[data-testid="view-board"]').trigger('click')

      expect(wrapper.findComponent(BoardView).exists()).toBe(true)
      expect(wrapper.findComponent(ScheduleDays).exists()).toBe(false)
      expect(wrapper.find('[data-testid="view-board"]').attributes('aria-pressed')).toBe('true')
    })

    it('moves an activity dropped on the board', async () => {
      mockLoadedTrip()
      vi.mocked(moveActivity).mockResolvedValue({ ...shibuya, date: '2026-12-02' })
      const wrapper = await mountView()
      await wrapper.find('[data-testid="view-board"]').trigger('click')

      wrapper
        .findComponent(BoardView)
        .vm.$emit('move', { activityId: 'shibuya-id', date: '2026-12-02', startTime: '15:00' })
      await flushPromises()

      expect(moveActivity).toHaveBeenCalledWith('tokyo-id', 'shibuya-id', {
        date: '2026-12-02',
        startTime: '15:00',
      })
      expect(wrapper.findComponent(BoardView).props('days')[1]?.activities[0]?.name).toBe('Shibuya')
    })

    it('shows the error when a move is rejected', async () => {
      mockLoadedTrip()
      vi.mocked(moveActivity).mockRejectedValue(new Error('The activity would end after midnight.'))
      const wrapper = await mountView()
      await wrapper.find('[data-testid="view-board"]').trigger('click')

      wrapper
        .findComponent(BoardView)
        .vm.$emit('move', { activityId: 'shibuya-id', date: '2026-12-01', startTime: '23:30' })
      await flushPromises()

      expect(wrapper.find('[data-testid="schedule-error"]').text()).toContain(
        'The activity would end after midnight.',
      )
    })
  })

  it('links to the trip overview', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()
    const link = wrapper
      .findAllComponents(RouterLinkStub)
      .find((item) => item.attributes('data-testid') === 'open-overview')

    expect(link?.props('to')).toEqual({ name: 'trip-overview', params: { id: 'tokyo-id' } })
  })

  describe('quick stays', () => {
    const inn = {
      id: 'inn',
      name: 'Asakusa Inn',
      checkIn: '2026-12-01',
      checkOut: '2026-12-02',
      nights: 1,
      cost: 3000,
      currency: 'TWD',
      costInTwd: 3000,
      bookingStatus: 'Booked' as const,
      confirmationCode: '',
    }

    function mockTripWithOneNight() {
      mockLoadedTrip()
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: 'tokyo-id',
        ratesAvailable: true,
        ratesUpdatedAt: '2026-01-01T00:00:00Z',
        stays: [inn],
        days: [
          { date: '2026-12-01', activities: [], stay: inn },
          { date: '2026-12-02', activities: [], stay: null, checkingOut: inn },
        ],
      })
    }

    it("extends last night's stay at the same nightly price", async () => {
      mockTripWithOneNight()
      vi.mocked(updateStay).mockResolvedValue({ ...inn, checkOut: '2026-12-03', nights: 2 })
      const wrapper = await mountView()

      await wrapper.find('[data-testid="day-extend-stay"]').trigger('click')
      await flushPromises()

      expect(updateStay).toHaveBeenCalledWith('tokyo-id', 'inn', {
        name: 'Asakusa Inn',
        checkIn: '2026-12-01',
        checkOut: '2026-12-03',
        cost: 6000,
        currency: 'TWD',
        bookingStatus: 'Booked',
        confirmationCode: '',
      })
      expect(fetchSchedule).toHaveBeenCalledTimes(2)
      expect(wrapper.findComponent(StayDialog).props('open')).toBe(false)
    })

    it('opens the stay with the error when extending fails', async () => {
      mockTripWithOneNight()
      vi.mocked(updateStay).mockRejectedValue(new Error('These nights overlap with "Hotel".'))
      const wrapper = await mountView()

      await wrapper.find('[data-testid="day-extend-stay"]').trigger('click')
      await flushPromises()

      const dialog = wrapper.findComponent(StayDialog)
      expect(dialog.props('open')).toBe(true)
      expect(dialog.props('stay')).toMatchObject({ id: 'inn' })
      expect(dialog.props('error')).toBe('These nights overlap with "Hotel".')
    })
  })

  describe('views', () => {
    function fakeRouter(query: Record<string, string> = {}) {
      const replace = vi.fn<Router['replace']>()
      const router = { currentRoute: { value: { query } }, replace } as unknown as Router
      return { router, replace }
    }

    async function mountWithRouter(router: Router, props: Record<string, string> = {}) {
      const wrapper = mount(TripScheduleView, {
        props: { id: 'tokyo-id', ...props },
        global: {
          plugins: [createPinia()],
          stubs: { RouterLink: RouterLinkStub },
          provide: { [routerKey as symbol]: router },
        },
      })
      await flushPromises()
      return wrapper
    }

    it('switches to the table and keeps the view in the url', async () => {
      mockLoadedTrip()
      const { router, replace } = fakeRouter({ day: '2' })
      const wrapper = await mountWithRouter(router)

      await wrapper.find('[data-testid="view-table"]').trigger('click')

      expect(wrapper.findComponent(TableView).exists()).toBe(true)
      expect(wrapper.findComponent(ScheduleDays).exists()).toBe(false)
      expect(wrapper.find('[data-testid="view-table"]').attributes('aria-pressed')).toBe('true')
      expect(replace).toHaveBeenLastCalledWith({ query: { view: 'table' } })

      await wrapper.find('[data-testid="view-timeline"]').trigger('click')
      expect(replace).toHaveBeenLastCalledWith({ query: {} })
    })

    it('opens the view given in the url', async () => {
      mockLoadedTrip()
      const { router } = fakeRouter()

      const wrapper = await mountWithRouter(router, { view: 'board' })

      expect(wrapper.findComponent(BoardView).exists()).toBe(true)
    })

    it('remembers the last view of the trip', async () => {
      mockLoadedTrip()
      const first = await mountWithRouter(fakeRouter().router)
      await first.find('[data-testid="view-table"]').trigger('click')
      first.unmount()

      const second = await mountWithRouter(fakeRouter().router)

      expect(second.findComponent(TableView).exists()).toBe(true)
    })

    it('ignores an unknown view in the url', async () => {
      mockLoadedTrip()

      const wrapper = await mountWithRouter(fakeRouter().router, { view: 'map' })

      expect(wrapper.findComponent(ScheduleDays).exists()).toBe(true)
    })

    it('follows the url when it changes', async () => {
      mockLoadedTrip()
      const wrapper = await mountWithRouter(fakeRouter().router)

      await wrapper.setProps({ view: 'table' })

      expect(wrapper.findComponent(TableView).exists()).toBe(true)
    })

    it('edits an activity clicked in the table', async () => {
      mockLoadedTrip()
      const wrapper = await mountWithRouter(fakeRouter().router, { view: 'table' })

      await wrapper.find('[data-testid="table-row"]').trigger('click')

      expect(wrapper.findComponent(ActivityDialog).props('open')).toBe(true)
      expect(wrapper.findComponent(ActivityDialog).props('activity')).toMatchObject({
        id: 'shibuya-id',
      })
    })
  })

  describe('booking filter', () => {
    it('filters every view by booking status', async () => {
      mockLoadedTrip()
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: 'tokyo-id',
        ratesAvailable: true,
        ratesUpdatedAt: '2026-01-01T00:00:00Z',
        days: [
          {
            date: '2026-12-01',
            activities: [
              shibuya,
              {
                ...shibuya,
                id: 'sushi',
                name: 'Sushi',
                startTime: '19:00:00',
                endTime: '20:00:00',
                bookingStatus: 'ToBook',
              },
            ],
          },
          { date: '2026-12-02', activities: [] },
        ],
      })
      const wrapper = await mountView()
      const filter = wrapper.find('[data-testid="booking-filter"]')

      await filter.find('[data-testid="select-trigger"]').trigger('click')
      await filter
        .findAll('[role="option"]')
        .find((option) => option.text() === 'To book')!
        .trigger('click')

      expect(activityNames(wrapper)).toEqual(['Sushi'])
      await wrapper.find('[data-testid="view-table"]').trigger('click')
      expect(wrapper.findAll('[data-testid="table-name"]').map((cell) => cell.text())).toEqual([
        'Sushi',
      ])
    })

    it('is hidden when every activity has the same booking status', async () => {
      mockLoadedTrip()

      const wrapper = await mountView()

      expect(wrapper.find('[data-testid="booking-filter"]').exists()).toBe(false)
    })
  })

  describe('category filter', () => {
    function mockMixedTrip() {
      mockLoadedTrip()
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: 'tokyo-id',
        ratesAvailable: true,
        ratesUpdatedAt: '2026-01-01T00:00:00Z',
        days: [
          {
            date: '2026-12-01',
            activities: [
              { ...shibuya, category: 'Sightseeing' },
              {
                ...shibuya,
                id: 'sushi',
                name: 'Sushi',
                startTime: '19:00:00',
                endTime: '20:00:00',
                category: 'Food',
              },
            ],
          },
          { date: '2026-12-02', activities: [] },
        ],
      })
    }

    it('offers only the categories used in the trip', async () => {
      mockMixedTrip()

      const wrapper = await mountView()

      expect(wrapper.find('[data-testid="filter-Sightseeing"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="filter-Food"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="filter-Shopping"]').exists()).toBe(false)
    })

    it('hides the filter when only one category is used', async () => {
      mockLoadedTrip()

      const wrapper = await mountView()

      expect(wrapper.find('[data-testid="filter-all"]').exists()).toBe(false)
    })

    it('filters the timeline and the board', async () => {
      mockMixedTrip()
      const wrapper = await mountView()

      await wrapper.find('[data-testid="filter-Food"]').trigger('click')
      expect(activityNames(wrapper)).toEqual(['Sushi'])

      await wrapper.find('[data-testid="view-board"]').trigger('click')
      expect(wrapper.findAll('[data-testid="board-card-name"]').map((item) => item.text())).toEqual(
        ['Sushi'],
      )

      await wrapper.find('[data-testid="filter-all"]').trigger('click')
      expect(wrapper.findAll('[data-testid="board-card-name"]')).toHaveLength(2)
    })
  })
})

describe('TripScheduleView add flow', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.stubGlobal('localStorage', createMemoryStorage())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('keeps the form closed until an add button is used', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    expect(wrapper.findComponent(ActivityDialog).props('open')).toBe(false)
    expect(wrapper.findComponent(ActivityForm).exists()).toBe(false)
  })

  it('opens the dialog for the day whose add button was used', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()

    await wrapper.findAll('[data-testid="add-to-day"]')[1]!.trigger('click')

    expect(wrapper.findComponent(ActivityDialog).props('initialDate')).toBe('2026-12-02')
    expect(wrapper.findComponent(ActivityForm).props('initialDate')).toBe('2026-12-02')
    expect(wrapper.find('[data-testid="activity-dialog"]').text()).toContain('Day 2 · Wed, Dec 2')
  })

  it('opens the dialog for the first day from the header button', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()

    await wrapper.find('[data-testid="add-activity"]').trigger('click')

    expect(wrapper.findComponent(ActivityForm).props('initialDate')).toBe('2026-12-01')
  })

  it('opens the dialog from a board column', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()
    await wrapper.find('[data-testid="view-board"]').trigger('click')

    await wrapper.findAll('[data-testid="board-add"]')[1]!.trigger('click')

    expect(wrapper.findComponent(ActivityForm).props('initialDate')).toBe('2026-12-02')
  })

  it('closes the dialog after adding', async () => {
    mockLoadedTrip()
    vi.mocked(addActivity).mockResolvedValue({ ...shibuya, id: 'new', name: 'Senso-ji' })
    const wrapper = await mountView()

    await submitActivity(wrapper, { name: 'Senso-ji', startTime: '10:00', endTime: '11:00' })

    expect(wrapper.find('[data-testid="activity-dialog"]').exists()).toBe(false)
  })

  it('keeps the dialog open with the error when adding fails', async () => {
    mockLoadedTrip()
    vi.mocked(addActivity).mockRejectedValue(new Error('The activity overlaps with "Shibuya"'))
    const wrapper = await mountView()

    await submitActivity(wrapper, { name: 'Hachiko', startTime: '15:30', endTime: '16:30' })

    const dialog = wrapper.find('[data-testid="activity-dialog"]')
    expect(dialog.exists()).toBe(true)
    expect(dialog.find('[data-testid="schedule-error"]').text()).toContain('overlaps')
  })

  it('closes the dialog with the close button', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()
    await wrapper.find('[data-testid="add-activity"]').trigger('click')

    await wrapper.find('[data-testid="close-activity-dialog"]').trigger('click')

    expect(wrapper.find('[data-testid="activity-dialog"]').exists()).toBe(false)
  })

  it('shows board move errors on the page', async () => {
    mockLoadedTrip()
    vi.mocked(moveActivity).mockRejectedValue(new Error('The activity would end after midnight.'))
    const wrapper = await mountView()
    await wrapper.find('[data-testid="view-board"]').trigger('click')

    wrapper
      .findComponent(BoardView)
      .vm.$emit('move', { activityId: 'shibuya-id', date: '2026-12-01', startTime: '23:30' })
    await flushPromises()

    expect(wrapper.find('[data-testid="activity-dialog"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="schedule-error"]').text()).toContain('after midnight')
  })

  it('keeps the trip bar with the trip summary', async () => {
    mockLoadedTrip()

    const wrapper = await mountView()

    const bar = wrapper.find('[data-testid="trip-bar"]')
    expect(bar.find('h1').text()).toBe('Tokyo')
    expect(bar.find('[data-testid="trip-total"]').exists()).toBe(true)
    expect(bar.find('[data-testid="add-activity"]').exists()).toBe(true)
  })
})

import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DOMWrapper, flushPromises, mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { addActivity, fetchSchedule, updateActivity } from '@/api/activities'
import { fetchExchangeRates } from '@/api/exchangeRates'
import { fetchTrip, updateTrip } from '@/api/trips'
import type { Activity } from '@/types/activity'
import ActivityForm from '@/components/ActivityForm.vue'
import TripEditDialog from '@/components/TripEditDialog.vue'
import { createMemoryStorage } from '@/test/memoryStorage'
import TripScheduleView from '../TripScheduleView.vue'

vi.mock('@/api/activities')
vi.mock('@/api/trips')
vi.mock('@/api/exchangeRates')

const shibuya: Activity = {
  id: 'shibuya-id',
  name: 'Shibuya',
  date: '2026-12-01',
  startTime: '15:00:00',
  endTime: '16:00:00',
  cost: 2200,
  currency: 'TWD',
  costInTwd: 2200,
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

function activityNames(wrapper: Awaited<ReturnType<typeof mountView>>): string[] {
  return wrapper.findAll('[data-testid="activity-name"]').map((item) => item.text())
}

describe('TripScheduleView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
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
    expect(wrapper.find('.section-title').text()).toBe('Edit "Shibuya"')
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
    })
    expect(activityNames(wrapper)).toEqual(['Shibuya Sky'])
    expect(wrapper.find('.section-title').text()).toBe('Add activity')
  })

  it('stays in edit mode when saving fails', async () => {
    mockLoadedTrip()
    vi.mocked(updateActivity).mockRejectedValue(new Error('Overlap'))
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-activity"]').trigger('click')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('.section-title').text()).toBe('Edit "Shibuya"')
    expect(wrapper.find('[data-testid="schedule-error"]').text()).toContain('Overlap')
  })

  it('cancels editing', async () => {
    mockLoadedTrip()
    const wrapper = await mountView()

    await wrapper.find('[data-testid="edit-activity"]').trigger('click')
    await wrapper.find('[data-testid="cancel-edit"]').trigger('click')

    expect(wrapper.find('.section-title').text()).toBe('Add activity')
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
    })
    const wrapper = await mountView()

    await wrapper.findComponent(ActivityForm).props('submit')({
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00',
      endTime: '11:00',
      cost: 500,
      currency: 'JPY',
    })
    await flushPromises()

    expect(localStorage.getItem('tripwise.currency.tokyo-id')).toBe('JPY')
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

    expect(wrapper.findComponent(ActivityForm).props('near')).toEqual({
      latitude: 35.658,
      longitude: 139.702,
    })
  })
})

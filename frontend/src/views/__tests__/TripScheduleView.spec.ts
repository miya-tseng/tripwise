import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DOMWrapper, flushPromises, mount, RouterLinkStub, type VueWrapper } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { addActivity, fetchSchedule, updateActivity } from '@/api/activities'
import { fetchTrip } from '@/api/trips'
import type { Activity } from '@/types/activity'
import ActivityForm from '@/components/ActivityForm.vue'
import TripScheduleView from '../TripScheduleView.vue'

vi.mock('@/api/activities')
vi.mock('@/api/trips')

const shibuya: Activity = {
  id: 'shibuya-id',
  name: 'Shibuya',
  date: '2026-12-01',
  startTime: '15:00:00',
  endTime: '16:00:00',
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
  values: { name: string; startTime: string; endTime: string },
) {
  await wrapper.find('input[name="name"]').setValue(values.name)
  await pickTime(wrapper, 'startTime', values.startTime)
  await pickTime(wrapper, 'endTime', values.endTime)
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
    })
    const wrapper = await mountView()

    await submitActivity(wrapper, { name: 'Senso-ji', startTime: '10:00', endTime: '11:00' })

    expect(addActivity).toHaveBeenCalledWith('tokyo-id', {
      name: 'Senso-ji',
      date: '2026-12-01',
      startTime: '10:00',
      endTime: '11:00',
    })
    expect(activityNames(wrapper)).toEqual(['Senso-ji', 'Shibuya'])
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
})

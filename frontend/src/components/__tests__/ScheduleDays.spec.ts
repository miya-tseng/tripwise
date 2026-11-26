import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ScheduleDays from '../ScheduleDays.vue'
import ConfirmDialog from '../ConfirmDialog.vue'
import DayMap from '../DayMap.vue'
import { hasMapsKey } from '@/utils/googleMaps'
import type { Activity, ScheduleDay } from '@/types/activity'

vi.mock('@/utils/googleMaps', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/utils/googleMaps')>()),
  hasMapsKey: vi.fn<() => boolean>(() => false),
  loadGoogleMaps: vi.fn<() => Promise<never>>(() => new Promise(() => {})),
}))

function activity(
  id: string,
  date: string,
  startTime: string,
  endTime: string,
  cost = 0,
): Activity {
  return {
    id,
    name: id,
    date,
    startTime,
    endTime,
    cost,
    currency: 'TWD',
    costInTwd: cost,
    category: 'Other',
    bookingStatus: 'NotNeeded',
    notes: '',
    confirmationCode: '',
    link: null,
  }
}

const tsukiji = activity('Tsukiji', '2026-12-01', '07:00:00', '08:00:00', 1500.5)
const sensoji = activity('Senso-ji', '2026-12-01', '09:00:00', '10:30:00')
const tower = activity('Tokyo Tower', '2026-12-03', '18:00:00', '19:00:00', 0.1)

const days: ScheduleDay[] = [
  { date: '2026-12-01', activities: [tsukiji, sensoji] },
  { date: '2026-12-02', activities: [] },
  { date: '2026-12-03', activities: [tower] },
]

function mountDays(props: { editingId?: string | null; deletingId?: string | null } = {}) {
  return mount(ScheduleDays, { props: { days, ...props } })
}

describe('ScheduleDays', () => {
  it('shows every day in order with its number and date', () => {
    const wrapper = mountDays()

    const dayItems = wrapper.findAll('[data-testid="schedule-day"]')
    expect(dayItems).toHaveLength(3)
    expect(dayItems[0]!.text()).toContain('Day 1')
    expect(dayItems[0]!.find('[data-testid="day-date"]').text()).toBe('Tue, Dec 1')
    expect(dayItems[2]!.text()).toContain('Day 3')
    expect(dayItems[2]!.find('[data-testid="day-date"]').text()).toBe('Thu, Dec 3')
  })

  it('shows the activities of each day in the given order with their times', () => {
    const wrapper = mountDays()

    const firstDay = wrapper.findAll('[data-testid="schedule-day"]')[0]!
    const names = firstDay.findAll('[data-testid="activity-name"]').map((item) => item.text())
    const times = firstDay.findAll('[data-testid="activity-time"]').map((item) => item.text())
    expect(names).toEqual(['Tsukiji', 'Senso-ji'])
    expect(times).toEqual(['07:00 – 08:00', '09:00 – 10:30'])
    expect(firstDay.text()).toContain('2 activities')
  })

  it('shows the cost of each activity with two decimals or Free', () => {
    const wrapper = mountDays()

    const costs = wrapper.findAll('[data-testid="activity-cost"]').map((item) => item.text())
    expect(costs).toEqual(['NT$1,500.50', 'Free', 'NT$0.10'])
  })

  it('shows the subtotal of each day', () => {
    const wrapper = mountDays()

    const totals = wrapper.findAll('[data-testid="day-total"]').map((item) => item.text())
    expect(totals).toEqual(['NT$1,500.50', 'NT$0.00', 'NT$0.10'])
  })

  it('shows an empty message for a day without activities', () => {
    const wrapper = mountDays()

    const secondDay = wrapper.findAll('[data-testid="schedule-day"]')[1]!
    expect(secondDay.find('[data-testid="day-empty"]').text()).toBe('Nothing planned yet.')
    expect(secondDay.findAll('[data-testid="activity-item"]')).toHaveLength(0)
  })

  it('emits edit with the activity', async () => {
    const wrapper = mountDays()

    await wrapper.findAll('[data-testid="edit-activity"]')[1]!.trigger('click')

    expect(wrapper.emitted('edit')).toEqual([[sensoji]])
  })

  it('highlights the activity being edited', () => {
    const wrapper = mountDays({ editingId: sensoji.id })

    const items = wrapper.findAll('[data-testid="activity-item"]')
    expect(items[0]!.classes()).not.toContain('editing')
    expect(items[1]!.classes()).toContain('editing')
  })

  it('asks for confirmation and emits delete when confirmed', async () => {
    const wrapper = mountDays()

    await wrapper.findAll('[data-testid="delete-activity"]')[0]!.trigger('click')
    const dialog = wrapper.findComponent(ConfirmDialog)
    expect(dialog.props('open')).toBe(true)
    expect(dialog.props('message')).toBe('Delete "Tsukiji" from your schedule?')
    expect(wrapper.emitted('delete')).toBeUndefined()

    dialog.vm.$emit('confirm')
    await wrapper.vm.$nextTick()

    expect(wrapper.emitted('delete')).toEqual([[tsukiji.id]])
    expect(wrapper.findComponent(ConfirmDialog).props('open')).toBe(false)
  })

  it('does not emit delete when the deletion is cancelled', async () => {
    const wrapper = mountDays()

    await wrapper.findAll('[data-testid="delete-activity"]')[0]!.trigger('click')
    wrapper.findComponent(ConfirmDialog).vm.$emit('cancel')
    await wrapper.vm.$nextTick()

    expect(wrapper.emitted('delete')).toBeUndefined()
    expect(wrapper.findComponent(ConfirmDialog).props('open')).toBe(false)
  })

  it('disables the delete button while the activity is being deleted', () => {
    const wrapper = mountDays({ deletingId: tsukiji.id })

    const button = wrapper.findAll('[data-testid="delete-activity"]')[0]!
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Deleting...')
  })

  it('shows foreign costs in their currency with the TWD amount', () => {
    const shibuya: Activity = {
      id: 'shibuya',
      name: 'Shibuya Sky',
      date: '2026-12-01',
      startTime: '17:00:00',
      endTime: '18:00:00',
      cost: 3000,
      currency: 'JPY',
      costInTwd: 625,
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    }
    const wrapper = mount(ScheduleDays, {
      props: { days: [{ date: '2026-12-01', activities: [tsukiji, shibuya] }] },
    })

    expect(wrapper.findAll('[data-testid="activity-cost"]').map((item) => item.text())).toEqual([
      'NT$1,500.50',
      '¥3,000',
    ])
    expect(wrapper.findAll('[data-testid="activity-cost-twd"]').map((item) => item.text())).toEqual(
      ['≈ NT$625.00'],
    )
    expect(wrapper.find('[data-testid="day-total"]').text()).toBe('NT$2,125.50')
  })

  it('shows the day total per currency when a cost cannot be converted', () => {
    const shibuya: Activity = {
      id: 'shibuya',
      name: 'Shibuya Sky',
      date: '2026-12-01',
      startTime: '17:00:00',
      endTime: '18:00:00',
      cost: 3000,
      currency: 'JPY',
      costInTwd: null,
      category: 'Other',
      bookingStatus: 'NotNeeded',
      notes: '',
      confirmationCode: '',
      link: null,
    }
    const wrapper = mount(ScheduleDays, {
      props: { days: [{ date: '2026-12-01', activities: [tsukiji, shibuya] }] },
    })

    expect(wrapper.find('[data-testid="activity-cost-twd"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="day-total"]').text()).toBe('NT$1,500.50 + ¥3,000')
  })

  it('shows the address of activities with a place', () => {
    const withPlace: Activity = {
      ...tsukiji,
      place: {
        placeId: 'tsukiji-id',
        name: 'Tsukiji Outer Market',
        address: '4 Chome-16-2 Tsukiji, Chuo City, Tokyo',
        latitude: 35.66,
        longitude: 139.77,
        googleMapsUri: 'https://maps.google.com/?cid=2',
      },
    }
    const wrapper = mount(ScheduleDays, {
      props: { days: [{ date: '2026-12-01', activities: [withPlace, sensoji] }] },
    })

    expect(wrapper.findAll('[data-testid="activity-address"]').map((item) => item.text())).toEqual([
      '4 Chome-16-2 Tsukiji, Chuo City, Tokyo',
    ])
  })

  describe('Google Maps links', () => {
    function placeOf(id: string, latitude: number, longitude: number) {
      return {
        placeId: id,
        name: id,
        address: `${id} address`,
        latitude,
        longitude,
        googleMapsUri: `https://maps.google.com/?cid=${id}`,
      }
    }

    const morning: Activity = {
      ...tsukiji,
      id: 'morning',
      place: placeOf('Tsukiji', 35.66, 139.77),
    }
    const freeTime: Activity = { ...sensoji, id: 'free', name: 'Free time', place: null }
    const evening: Activity = {
      ...tower,
      id: 'evening',
      date: '2026-12-01',
      place: placeOf('Tokyo Tower', 35.65, 139.74),
    }

    function mountWithPlaces(activities: Activity[]) {
      return mount(ScheduleDays, { props: { days: [{ date: '2026-12-01', activities }] } })
    }

    it('links activities with a place to Google Maps in a new tab', () => {
      const wrapper = mountWithPlaces([morning, freeTime])

      const links = wrapper.findAll('[data-testid="activity-map-link"]')
      expect(links).toHaveLength(1)
      expect(links[0]!.attributes('href')).toBe('https://maps.google.com/?cid=Tsukiji')
      expect(links[0]!.attributes('target')).toBe('_blank')
      expect(links[0]!.attributes('rel')).toBe('noopener noreferrer')
      expect(links[0]!.attributes('aria-label')).toBe('Open Tsukiji in Google Maps')
    })

    it('shows directions from the previous place, skipping activities without a place', () => {
      const wrapper = mountWithPlaces([morning, freeTime, evening])

      const directions = wrapper.findAll('[data-testid="directions-link"]')
      expect(directions).toHaveLength(1)
      expect(directions[0]!.text()).toBe('Directions from Tsukiji ↗')
      const url = new URL(directions[0]!.attributes('href')!)
      expect(url.searchParams.get('origin_place_id')).toBe('Tsukiji')
      expect(url.searchParams.get('destination_place_id')).toBe('Tokyo Tower')
    })

    it('shows the directions right before the activity they lead to', () => {
      const wrapper = mountWithPlaces([morning, evening])

      const rows = wrapper.findAll('.timeline > li').map((row) => row.attributes('data-testid'))
      expect(rows).toEqual(['activity-item', 'directions', 'activity-item'])
    })

    it('links the whole day as a route when it has two or more places', () => {
      const wrapper = mountWithPlaces([morning, freeTime, evening])

      const route = new URL(wrapper.find('[data-testid="day-route-link"]').attributes('href')!)
      expect(route.searchParams.get('origin_place_id')).toBe('Tsukiji')
      expect(route.searchParams.get('destination_place_id')).toBe('Tokyo Tower')
    })

    it('has no directions or day route with fewer than two places', () => {
      const wrapper = mountWithPlaces([morning, freeTime])

      expect(wrapper.find('[data-testid="directions-link"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="day-route-link"]').exists()).toBe(false)
    })
  })

  describe('day map', () => {
    const placed: Activity = {
      ...tsukiji,
      place: {
        placeId: 'tsukiji',
        name: 'Tsukiji',
        address: 'Tsukiji, Tokyo',
        latitude: 35.66,
        longitude: 139.77,
        googleMapsUri: 'https://maps.google.com/?cid=tsukiji',
      },
    }

    beforeEach(() => {
      vi.mocked(hasMapsKey).mockReturnValue(true)
    })

    function mountMapDays(activities: Activity[]) {
      return mount(ScheduleDays, {
        props: { days: [{ date: '2026-12-01', activities }] },
        attachTo: document.body,
      })
    }

    it('has no map toggle without a browser key', () => {
      vi.mocked(hasMapsKey).mockReturnValue(false)

      const wrapper = mountMapDays([placed])

      expect(wrapper.find('[data-testid="toggle-day-map"]').exists()).toBe(false)
      wrapper.unmount()
    })

    it('has no map toggle for a day without places', () => {
      const wrapper = mountMapDays([sensoji])

      expect(wrapper.find('[data-testid="toggle-day-map"]').exists()).toBe(false)
      wrapper.unmount()
    })

    it('loads the map only when it is opened', async () => {
      const wrapper = mountMapDays([placed, sensoji])
      expect(wrapper.findComponent(DayMap).exists()).toBe(false)

      await wrapper.find('[data-testid="toggle-day-map"]').trigger('click')

      const map = wrapper.findComponent(DayMap)
      expect(map.props('stops')).toEqual([
        { activityId: placed.id, name: placed.name, place: placed.place },
      ])
      expect(wrapper.find('[data-testid="toggle-day-map"]').text()).toBe('Hide map')
      expect(wrapper.find('[data-testid="toggle-day-map"]').attributes('aria-expanded')).toBe(
        'true',
      )

      await wrapper.find('[data-testid="toggle-day-map"]').trigger('click')
      expect(wrapper.findComponent(DayMap).exists()).toBe(false)
      wrapper.unmount()
    })

    it('highlights the activity picked on the map', async () => {
      const wrapper = mountMapDays([placed, sensoji])
      await wrapper.find('[data-testid="toggle-day-map"]').trigger('click')

      wrapper.findComponent(DayMap).vm.$emit('select', placed.id)
      await wrapper.vm.$nextTick()

      const items = wrapper.findAll('[data-testid="activity-item"]')
      expect(items[0]!.classes()).toContain('highlighted')
      expect(items[1]!.classes()).not.toContain('highlighted')
      expect(wrapper.findComponent(DayMap).props('highlightedId')).toBe(placed.id)
      wrapper.unmount()
    })
  })

  describe('ideas', () => {
    const placed: Activity = {
      ...tsukiji,
      place: {
        placeId: 'tsukiji',
        name: 'Tsukiji',
        address: 'Tsukiji, Tokyo',
        latitude: 35.66,
        longitude: 139.77,
        googleMapsUri: 'https://maps.google.com/?cid=tsukiji',
      },
    }

    it('offers ideas only for activities with a place', async () => {
      const wrapper = mount(ScheduleDays, {
        props: { days: [{ date: '2026-12-01', activities: [placed, sensoji] }] },
      })

      const buttons = wrapper.findAll('[data-testid="suggest-activity"]')
      expect(buttons).toHaveLength(1)

      await buttons[0]!.trigger('click')

      expect(wrapper.emitted('suggest')).toEqual([[placed]])
    })
  })

  describe('activity details', () => {
    function mountOne(overrides: Partial<Activity>) {
      return mount(ScheduleDays, {
        props: { days: [{ date: '2026-12-01', activities: [{ ...tsukiji, ...overrides }] }] },
      })
    }

    it('shows the category icon and colors the row', () => {
      const wrapper = mountOne({ category: 'Food' })

      expect(wrapper.find('[data-testid="activity-category"]').text()).toBe('🍜')
      expect(wrapper.find('[data-testid="activity-category"]').attributes('title')).toBe('Food')
      expect(wrapper.find('[data-testid="activity-item"]').classes()).toContain('category-food')
    })

    it.each([
      ['ToBook', 'To book', 'tone-warning'],
      ['Booked', 'Booked', 'tone-success'],
      ['Paid', 'Paid', 'tone-success'],
    ] as const)('shows a %s badge', (bookingStatus, label, tone) => {
      const wrapper = mountOne({ bookingStatus })

      expect(wrapper.find('[data-testid="booking-badge"]').text()).toBe(label)
      expect(wrapper.find('[data-testid="booking-badge"]').classes()).toContain(tone)
    })

    it('has no badge when no booking is needed', () => {
      expect(mountOne({}).find('[data-testid="booking-badge"]').exists()).toBe(false)
    })

    it('previews the notes and links the booking', () => {
      const wrapper = mountOne({
        notes: 'Bring passport\nArrive early',
        confirmationCode: 'AB12',
        link: 'https://example.com/booking',
      })

      expect(wrapper.find('[data-testid="activity-notes"]').text()).toBe('Bring passport')
      const link = wrapper.find('[data-testid="activity-link"]')
      expect(link.text()).toBe('Booking AB12 ↗')
      expect(link.attributes('href')).toBe('https://example.com/booking')
      expect(link.attributes('target')).toBe('_blank')
    })

    it('shows the confirmation code without a link', () => {
      const wrapper = mountOne({ confirmationCode: 'AB12' })

      expect(wrapper.find('[data-testid="activity-confirmation"]').text()).toBe('Booking AB12')
    })
  })
})

import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ScheduleDays from '../ScheduleDays.vue'
import ConfirmDialog from '../ConfirmDialog.vue'
import type { Activity, ScheduleDay } from '@/types/activity'

function activity(
  id: string,
  date: string,
  startTime: string,
  endTime: string,
  cost = 0,
): Activity {
  return { id, name: id, date, startTime, endTime, cost, currency: 'TWD', costInTwd: cost }
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
})

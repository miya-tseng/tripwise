import { describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import TableView from '../TableView.vue'
import type { Activity, ScheduleDay } from '@/types/activity'

function activity(
  name: string,
  date: string,
  startTime: string,
  overrides: Partial<Activity> = {},
): Activity {
  return {
    id: `${name}-id`,
    name,
    date,
    startTime,
    endTime: '23:00:00',
    cost: 0,
    currency: 'TWD',
    costInTwd: 0,
    category: 'Other',
    bookingStatus: 'NotNeeded',
    notes: '',
    confirmationCode: '',
    link: null,
    ...overrides,
  }
}

const sushi = activity('Sushi Dai', '2026-12-01', '07:00:00', {
  endTime: '08:00:00',
  category: 'Food',
  bookingStatus: 'ToBook',
  cost: 3000,
  currency: 'JPY',
  costInTwd: 630,
})
const sensoji = activity('Senso-ji', '2026-12-01', '09:00:00', {
  category: 'Sightseeing',
  place: {
    placeId: 'senso',
    name: 'Senso-ji Temple',
    address: 'Asakusa',
    latitude: 35.7,
    longitude: 139.8,
    googleMapsUri: 'https://maps.google.com/?cid=2',
  },
})
const tower = activity('Tokyo Tower', '2026-12-03', '18:00:00', {
  bookingStatus: 'Paid',
  cost: 1200,
  costInTwd: 1200,
})

const days: ScheduleDay[] = [
  { date: '2026-12-01', activities: [sensoji, sushi] },
  { date: '2026-12-02', activities: [] },
  { date: '2026-12-03', activities: [tower] },
]

function names(wrapper: VueWrapper) {
  return wrapper.findAll('[data-testid="table-name"]').map((cell) => cell.text())
}

describe('TableView', () => {
  it('lists activities by day with a subtotal per day', () => {
    const wrapper = mount(TableView, { props: { days } })

    expect(wrapper.findAll('[data-testid="table-day"]').map((row) => row.text())).toEqual([
      expect.stringContaining('Day 1 · Tue, Dec 1'),
      expect.stringContaining('Day 3 · Thu, Dec 3'),
    ])
    expect(wrapper.findAll('[data-testid="table-day-total"]').map((cell) => cell.text())).toEqual([
      'NT$630.00',
      'NT$1,200.00',
    ])
    expect(names(wrapper)).toEqual(['Sushi Dai', 'Senso-ji', 'Tokyo Tower'])
  })

  it('shows time, category, booking, cost in both currencies and place', () => {
    const wrapper = mount(TableView, { props: { days } })
    const [sushiRow, sensojiRow] = wrapper.findAll('[data-testid="table-row"]')

    expect(sushiRow!.text()).toContain('07:00–08:00')
    expect(sushiRow!.text()).toContain('🍜 Food')
    expect(sushiRow!.find('.booking-badge').text()).toBe('To book')
    expect(sushiRow!.find('[data-testid="table-cost"]').text()).toBe('¥3,000 ≈ NT$630.00')
    expect(sensojiRow!.text()).toContain('Senso-ji Temple')
    expect(sensojiRow!.find('.booking-badge').exists()).toBe(false)
    expect(sensojiRow!.find('[data-testid="table-cost"]').text()).toBe('')
  })

  it('sorts by a column and toggles the direction', async () => {
    const wrapper = mount(TableView, { props: { days } })
    const nameHeader = () => wrapper.find('[data-testid="sort-name"]')

    await nameHeader().trigger('click')
    expect(names(wrapper)).toEqual(['Senso-ji', 'Sushi Dai', 'Tokyo Tower'])
    expect(nameHeader().element.closest('th')!.getAttribute('aria-sort')).toBe('ascending')
    expect(wrapper.find('[data-testid="table-day"]').exists()).toBe(false)

    await nameHeader().trigger('click')
    expect(names(wrapper)).toEqual(['Tokyo Tower', 'Sushi Dai', 'Senso-ji'])
    expect(nameHeader().element.closest('th')!.getAttribute('aria-sort')).toBe('descending')
  })

  it('shows the most expensive first and a grand total when sorting by cost', async () => {
    const wrapper = mount(TableView, { props: { days } })

    await wrapper.find('[data-testid="sort-cost"]').trigger('click')

    expect(names(wrapper)).toEqual(['Tokyo Tower', 'Sushi Dai', 'Senso-ji'])
    expect(wrapper.find('[data-testid="table-total"]').text()).toBe('NT$1,830.00')
    expect(wrapper.findAll('[data-testid="table-row"]')[0]!.text()).toContain('Day 3')
  })

  it('emits edit when a row is clicked or entered', async () => {
    const wrapper = mount(TableView, { props: { days } })
    const rows = wrapper.findAll('[data-testid="table-row"]')

    await rows[0]!.trigger('click')
    await rows[2]!.trigger('keydown', { key: 'Enter' })

    expect(wrapper.emitted('edit')).toEqual([[sushi], [tower]])
  })

  it('shows a message when there is nothing to list', () => {
    const wrapper = mount(TableView, { props: { days: [{ date: '2026-12-01', activities: [] }] } })

    expect(wrapper.find('[data-testid="table-empty"]').text()).toBe('No activities to show.')
  })
})

import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { routerKey, type Router } from 'vue-router'
import { fetchOverview } from '@/api/overview'
import SpendByDayChart from '@/components/SpendByDayChart.vue'
import type { TripOverview } from '@/types/overview'
import TripOverviewView from '../TripOverviewView.vue'

vi.mock('@/api/overview')

const overview: TripOverview = {
  tripId: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-03',
  days: 3,
  nights: 3,
  nightsWithStay: 2,
  activityCount: 4,
  placeCount: 2,
  checklistDone: 0,
  checklistTotal: 0,
  ratesAvailable: true,
  totalInTwd: 10500,
  activitiesInTwd: 1500,
  staysInTwd: 9000,
  averagePerDayInTwd: 3500,
  spendByCurrency: [
    { currency: 'TWD', amount: 9500, amountInTwd: 9500 },
    { currency: 'JPY', amount: 4800, amountInTwd: 1000 },
  ],
  spendByCategory: [
    { category: 'Food', activityCount: 1, amountInTwd: 1000 },
    { category: 'Sightseeing', activityCount: 1, amountInTwd: 500 },
  ],
  spendByDay: [
    {
      date: '2026-12-01',
      dayNumber: 1,
      activityCount: 2,
      activitiesInTwd: 1500,
      stayInTwd: 4500,
      totalInTwd: 6000,
    },
    {
      date: '2026-12-02',
      dayNumber: 2,
      activityCount: 0,
      activitiesInTwd: 0,
      stayInTwd: 4500,
      totalInTwd: 4500,
    },
    {
      date: '2026-12-03',
      dayNumber: 3,
      activityCount: 0,
      activitiesInTwd: 0,
      stayInTwd: 0,
      totalInTwd: 0,
    },
  ],
  booking: { toBook: 1, booked: 2, paid: 1 },
  topExpenses: [
    {
      kind: 'Stay',
      id: 'hotel',
      name: 'Hotel',
      date: '2026-12-01',
      cost: { amount: 9000, currency: 'TWD' },
      costInTwd: 9000,
    },
    {
      kind: 'Activity',
      id: 'sushi',
      name: 'Sushi',
      date: '2026-12-01',
      cost: { amount: 4800, currency: 'JPY' },
      costInTwd: 1000,
    },
  ],
  attention: [
    { kind: 'ActivityToBook', date: '2026-12-01', itemId: 'sushi', name: 'Sushi' },
    { kind: 'NightWithoutStay', date: '2026-12-03', itemId: null, name: null },
  ],
}

async function mountView(router: Router | null = null) {
  const wrapper = mount(TripOverviewView, {
    props: { id: 'tokyo-id' },
    global: {
      stubs: { RouterLink: RouterLinkStub },
      provide: router ? { [routerKey as symbol]: router } : {},
    },
  })
  await flushPromises()
  return wrapper
}

describe('TripOverviewView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('shows the key facts', async () => {
    vi.mocked(fetchOverview).mockResolvedValue(overview)

    const wrapper = await mountView()

    expect(fetchOverview).toHaveBeenCalledWith('tokyo-id')
    expect(wrapper.find('h1').text()).toBe('Overview')
    expect(wrapper.find('[data-testid="header-trip-name"]').text()).toBe('Tokyo')
    expect(wrapper.find('[data-testid="total-spend"]').text()).toContain('NT$10,500.00')
    expect(wrapper.find('[data-testid="total-spend"]').text()).toContain('NT$3,500.00 per day')
    expect(wrapper.find('[data-testid="activity-total"]').text()).toContain('2 places')
    expect(wrapper.find('[data-testid="nights-covered"]').text()).toContain('2 / 3')
    expect(wrapper.find('[data-testid="nights-covered"]').text()).toContain('1 night to go')
    expect(wrapper.find('[data-testid="booking-progress"]').text()).toContain('3 / 4')
    expect(wrapper.find('[role="meter"]').attributes('aria-valuenow')).toBe('3')
  })

  it('shows where the money goes', async () => {
    vi.mocked(fetchOverview).mockResolvedValue(overview)

    const wrapper = await mountView()

    expect(wrapper.findComponent(SpendByDayChart).props('days')).toHaveLength(3)
    expect(wrapper.find('[data-testid="split-activities"]').text()).toBe('NT$1,500.00')
    expect(wrapper.find('[data-testid="split-stays"]').text()).toBe('NT$9,000.00')
    expect(wrapper.find('[data-testid="spend-split"]').text()).toContain('14%')
    expect(wrapper.find('[data-testid="by-currency"]').text()).toContain('≈ NT$1,000.00')
    expect(wrapper.findAll('[data-testid="top-expense"]').map((item) => item.text())).toEqual([
      expect.stringContaining('Hotel'),
      expect.stringContaining('Sushi'),
    ])
  })

  it('links every attention item to its day in the timeline', async () => {
    vi.mocked(fetchOverview).mockResolvedValue(overview)

    const wrapper = await mountView()
    const links = wrapper
      .findAllComponents(RouterLinkStub)
      .filter((link) => link.attributes('data-testid') === 'attention-item')

    expect(links.map((link) => link.text())).toEqual([
      expect.stringContaining('Book Sushi'),
      expect.stringContaining('No stay for the night'),
    ])
    expect(links[1]!.props('to')).toEqual({
      name: 'trip-schedule',
      params: { id: 'tokyo-id' },
      query: { view: 'timeline', day: '3' },
    })
  })

  it('links open checklist items to the checklist', async () => {
    vi.mocked(fetchOverview).mockResolvedValue({
      ...overview,
      attention: [
        { kind: 'ChecklistOpen', date: '2026-12-01', itemId: null, name: '2 checklist items left' },
      ],
    })

    const wrapper = await mountView()
    const link = wrapper
      .findAllComponents(RouterLinkStub)
      .find((item) => item.attributes('data-testid') === 'attention-item')!

    expect(link.text()).toContain('2 checklist items left')
    expect(link.props('to')).toEqual({ name: 'trip-checklist', params: { id: 'tokyo-id' } })
  })

  it('says all is set when nothing needs attention', async () => {
    vi.mocked(fetchOverview).mockResolvedValue({ ...overview, attention: [] })

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="all-set"]').exists()).toBe(true)
  })

  it('opens the day clicked in the chart', async () => {
    vi.mocked(fetchOverview).mockResolvedValue(overview)
    const push = vi.fn<Router['push']>()
    const wrapper = await mountView({ push } as unknown as Router)

    wrapper.findComponent(SpendByDayChart).vm.$emit('openDay', 2)

    expect(push).toHaveBeenCalledWith({
      name: 'trip-schedule',
      params: { id: 'tokyo-id' },
      query: { view: 'timeline', day: '2' },
    })
  })

  it('warns when exchange rates are unavailable', async () => {
    vi.mocked(fetchOverview).mockResolvedValue({
      ...overview,
      ratesAvailable: false,
      totalInTwd: null,
      averagePerDayInTwd: null,
    })

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="rates-warning"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="total-spend"]').text()).toContain('—')
  })

  it('shows the error when the overview cannot be loaded', async () => {
    vi.mocked(fetchOverview).mockRejectedValue(new Error('Trip not found.'))

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="overview-error"]').text()).toBe('Trip not found.')
  })
})

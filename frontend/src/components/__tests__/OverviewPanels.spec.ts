import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import OverviewTiles from '../OverviewTiles.vue'
import SpendSplit from '../SpendSplit.vue'
import TopExpensesList from '../TopExpensesList.vue'
import type { TripOverview } from '@/types/overview'

const empty: TripOverview = {
  tripId: 'trip',
  name: 'Trip',
  startDate: '2026-12-01',
  endDate: '2026-12-02',
  days: 2,
  nights: 2,
  nightsWithStay: 0,
  activityCount: 0,
  placeCount: 0,
  checklistDone: 0,
  checklistTotal: 0,
  ratesAvailable: true,
  totalInTwd: 0,
  activitiesInTwd: 0,
  staysInTwd: 0,
  averagePerDayInTwd: 0,
  spendByCurrency: [],
  spendByCategory: [],
  spendByDay: [],
  booking: { toBook: 0, booked: 0, paid: 0 },
  topExpenses: [],
  attention: [],
}

describe('OverviewTiles', () => {
  it('says nothing needs booking when there is nothing to book', () => {
    const tiles = mount(OverviewTiles, { props: { overview: empty } })
    const progress = tiles.get('[data-testid="booking-progress"]')

    expect(progress.text()).toContain('0 / 0')
    expect(progress.text()).toContain('Nothing needs booking')
    expect(progress.get('.meter-fill').attributes('style')).toContain('width: 0%')
  })

  it('fills the meter with the share of bookings done', () => {
    const tiles = mount(OverviewTiles, {
      props: { overview: { ...empty, booking: { toBook: 1, booked: 2, paid: 1 } } },
    })
    const progress = tiles.get('[data-testid="booking-progress"]')

    expect(progress.text()).toContain('3 / 4')
    expect(progress.text()).toContain('2 booked · 1 paid · ! 1 to book')
    expect(progress.get('.meter-fill').attributes('style')).toContain('width: 75%')
  })

  it('counts the nights still without a stay', () => {
    const tiles = mount(OverviewTiles, { props: { overview: { ...empty, nightsWithStay: 1 } } })

    expect(tiles.get('[data-testid="nights-covered"]').text()).toContain('1 / 2')
    expect(tiles.get('[data-testid="nights-covered"]').text()).toContain('1 night to go')
  })

  it('shows a dash when the total cannot be converted', () => {
    const tiles = mount(OverviewTiles, {
      props: { overview: { ...empty, totalInTwd: null, averagePerDayInTwd: null } },
    })

    expect(tiles.get('[data-testid="total-spend"]').text()).toContain('—')
  })
})

describe('SpendSplit', () => {
  it('says there are no costs yet', () => {
    const split = mount(SpendSplit, { props: { overview: empty } })

    expect(split.text()).toContain('No costs yet.')
    expect(split.text()).toContain('Nothing paid yet.')
    expect(split.find('[data-testid="by-currency"]').exists()).toBe(false)
  })

  it('draws only the stays part when nothing else costs money', () => {
    const split = mount(SpendSplit, {
      props: { overview: { ...empty, totalInTwd: 9000, staysInTwd: 9000 } },
    })

    expect(split.findAll('.split-part')).toHaveLength(1)
    expect(split.get('.split-part').classes()).toContain('stays')
  })
})

describe('TopExpensesList', () => {
  it('says there are no costs yet', () => {
    const list = mount(TopExpensesList, { props: { expenses: [] } })

    expect(list.text()).toContain('No costs yet.')
  })
})

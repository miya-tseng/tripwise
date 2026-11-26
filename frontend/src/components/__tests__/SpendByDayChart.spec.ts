import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import SpendByDayChart from '../SpendByDayChart.vue'
import type { DaySpend } from '@/types/overview'

const days: DaySpend[] = [
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
]

function heightOf(element: Element | undefined): string {
  return (element as HTMLElement | undefined)?.style.height ?? ''
}

describe('SpendByDayChart', () => {
  it('stacks activities and stays on a shared scale', () => {
    const wrapper = mount(SpendByDayChart, { props: { days } })
    const bars = wrapper.findAll('[data-testid="day-bar"]')

    expect(bars).toHaveLength(3)
    expect(heightOf(bars[0]!.find('[data-testid="day-bar-activities"]').element)).toBe('25%')
    expect(heightOf(bars[0]!.find('[data-testid="day-bar-stay"]').element)).toBe('75%')
    expect(bars[1]!.find('[data-testid="day-bar-activities"]').exists()).toBe(false)
    expect(bars[2]!.find('[data-testid="day-bar-stay"]').exists()).toBe(false)
  })

  it('labels each day for screen readers and offers a table', () => {
    const wrapper = mount(SpendByDayChart, { props: { days } })

    expect(wrapper.find('[data-testid="day-bar"]').attributes('aria-label')).toBe(
      'Day 1, Tue, Dec 1: NT$6,000.00',
    )
    expect(wrapper.findAll('table tbody tr')).toHaveLength(3)
  })

  it('shows the breakdown while hovering a day', async () => {
    const wrapper = mount(SpendByDayChart, { props: { days } })
    const bar = wrapper.find('[data-testid="day-bar"]')

    await bar.trigger('mouseenter')
    const tooltip = wrapper.find('[data-testid="day-tooltip"]')
    expect(tooltip.text()).toContain('Day 1 · Tue, Dec 1')
    expect(tooltip.text()).toContain('NT$1,500.00')
    expect(tooltip.text()).toContain('NT$4,500.00')
    expect(tooltip.text()).toContain('NT$6,000.00')

    await bar.trigger('mouseleave')
    expect(wrapper.find('[data-testid="day-tooltip"]').exists()).toBe(false)
  })

  it('opens the day when a bar is clicked', async () => {
    const wrapper = mount(SpendByDayChart, { props: { days } })

    await wrapper.findAll('[data-testid="day-bar"]')[1]!.trigger('click')

    expect(wrapper.emitted('openDay')).toEqual([[2]])
  })

  it('explains the empty chart when nothing costs money yet', () => {
    const empty = days.map((day) => ({ ...day, activitiesInTwd: 0, stayInTwd: 0, totalInTwd: 0 }))
    const wrapper = mount(SpendByDayChart, { props: { days: empty } })

    expect(wrapper.find('[data-testid="spend-by-day-empty"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="day-bar"]').exists()).toBe(false)
  })
})

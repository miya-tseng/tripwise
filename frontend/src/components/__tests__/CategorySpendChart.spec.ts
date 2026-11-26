import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import CategorySpendChart from '../CategorySpendChart.vue'
import type { CategorySpend } from '@/types/overview'

const categories: CategorySpend[] = [
  { category: 'Food', activityCount: 3, amountInTwd: 2000 },
  { category: 'Sightseeing', activityCount: 1, amountInTwd: 500 },
  { category: 'Other', activityCount: 2, amountInTwd: 0 },
]

describe('CategorySpendChart', () => {
  it('draws a bar per category with a cost, relative to the largest', () => {
    const wrapper = mount(CategorySpendChart, { props: { categories, total: 4000 } })
    const bars = wrapper.findAll('[data-testid="category-bar"]')

    expect(bars.map((bar) => bar.find('.bar-label').text())).toEqual(['🍜 Food', '🏯 Sightseeing'])
    expect(wrapper.findAll('[data-testid="category-value"]').map((v) => v.text())).toEqual([
      'NT$2,000.00',
      'NT$500.00',
    ])
    expect((bars[1]!.find('.fill').element as HTMLElement).style.width).toBe('25%')
  })

  it('lists categories without a cost separately', () => {
    const wrapper = mount(CategorySpendChart, { props: { categories, total: 4000 } })

    expect(wrapper.find('[data-testid="free-categories"]').text()).toBe('No cost: Other')
  })

  it('shows the count and share of the trip on hover', async () => {
    const wrapper = mount(CategorySpendChart, { props: { categories, total: 4000 } })

    await wrapper.find('[data-testid="category-bar"]').trigger('mouseenter')

    expect(wrapper.find('[data-testid="category-tooltip"]').text()).toBe(
      '3 activities · 50% of the trip',
    )
  })

  it('says so when no activity has a cost', () => {
    const wrapper = mount(CategorySpendChart, {
      props: { categories: [{ category: 'Other', activityCount: 1, amountInTwd: 0 }], total: 0 },
    })

    expect(wrapper.find('[data-testid="spend-by-category-empty"]').exists()).toBe(true)
  })
})

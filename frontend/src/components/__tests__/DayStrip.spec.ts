import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import DayStrip from '../DayStrip.vue'

const dates = ['2026-12-01', '2026-12-02', '2026-12-03']

describe('DayStrip', () => {
  it('shows a chip per day with the active day and today marked', () => {
    const wrapper = mount(DayStrip, { props: { dates, activeIndex: 1, today: '2026-12-03' } })
    const chips = wrapper.findAll('[data-testid="day-chip"]')

    expect(chips.map((chip) => chip.text())).toEqual(['D1Dec 1', 'D2Dec 2', 'D3Dec 3'])
    expect(chips[1]!.classes()).toContain('active')
    expect(chips[1]!.attributes('aria-current')).toBe('true')
    expect(chips[2]!.classes()).toContain('today')
    expect(chips[2]!.attributes('aria-label')).toBe('Day 3, Thu Dec 3, today')
  })

  it('asks to jump to the tapped day', async () => {
    const wrapper = mount(DayStrip, { props: { dates } })

    await wrapper.findAll('[data-testid="day-chip"]')[2]!.trigger('click')

    expect(wrapper.emitted('jump')).toEqual([[2]])
  })
})

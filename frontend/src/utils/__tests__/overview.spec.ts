import { describe, expect, it } from 'vitest'
import type { AttentionItem, DaySpend } from '@/types/overview'
import {
  attentionView,
  axisTicks,
  compactTwd,
  dayMax,
  niceMax,
  percentOf,
  share,
} from '../overview'

describe('attentionView', () => {
  const start = '2026-12-01'

  function item(kind: AttentionItem['kind'], date: string, name: string | null = null) {
    return { kind, date, itemId: name ? `${name}-id` : null, name }
  }

  it('describes an activity to book', () => {
    expect(attentionView(item('ActivityToBook', '2026-12-02', 'teamLab'), start)).toEqual({
      icon: '🎟️',
      title: 'Book teamLab',
      detail: 'Day 2 · Wed, Dec 2',
      dayNumber: 2,
    })
  })

  it('describes a stay to book by its check-in', () => {
    expect(attentionView(item('StayToBook', '2026-12-01', 'Inn'), start)).toEqual({
      icon: '🛏',
      title: 'Book Inn',
      detail: 'Check-in Day 1 · Tue, Dec 1',
      dayNumber: 1,
    })
  })

  it('describes a night without a stay and an empty day', () => {
    expect(attentionView(item('NightWithoutStay', '2026-12-03'), start).title).toBe(
      'No stay for the night',
    )
    expect(attentionView(item('EmptyDay', '2026-12-03'), start)).toMatchObject({
      title: 'Nothing planned',
      dayNumber: 3,
    })
  })
})

describe('chart scales', () => {
  it.each([
    [0, 0],
    [7, 8],
    [1800, 2000],
    [2300, 2400],
    [11400, 12000],
    [6000, 6000],
  ])('rounds %d up to %d', (value, expected) => {
    expect(niceMax(value)).toBe(expected)
  })

  it('spreads ticks evenly from zero', () => {
    expect(axisTicks(1000)).toEqual([0, 250, 500, 750, 1000])
    expect(axisTicks(0)).toEqual([0])
  })

  it('turns values into bar lengths', () => {
    expect(percentOf(250, 1000)).toBe(25)
    expect(percentOf(null, 1000)).toBe(0)
    expect(percentOf(5, 0)).toBe(0)
    expect(percentOf(2000, 1000)).toBe(100)
  })

  it('takes the scale from the busiest day', () => {
    const days = [{ totalInTwd: 1200 }, { totalInTwd: null }, { totalInTwd: 4300 }] as DaySpend[]

    expect(dayMax(days)).toBe(6000)
  })
})

describe('formatting', () => {
  it('shortens large amounts', () => {
    expect(compactTwd(2500)).toBe('NT$2.5K')
    expect(compactTwd(0)).toBe('NT$0')
  })

  it('rounds a share to a whole percent', () => {
    expect(share(1, 3)).toBe(33)
    expect(share(null, 3)).toBe(0)
    expect(share(1, 0)).toBe(0)
  })
})

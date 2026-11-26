import { describe, expect, it } from 'vitest'
import type { Activity, ScheduleDay } from '@/types/activity'
import {
  DEFAULT_SORT,
  filterDays,
  nextSort,
  parseView,
  tableGroups,
  type TableGroup,
} from '../activityTable'

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
  category: 'Food',
  bookingStatus: 'ToBook',
  cost: 3000,
  currency: 'JPY',
  costInTwd: 630,
})
const sensoji = activity('Senso-ji', '2026-12-01', '09:00:00', { category: 'Sightseeing' })
const tower = activity('Tokyo Tower', '2026-12-03', '18:00:00', {
  category: 'Sightseeing',
  bookingStatus: 'Paid',
  cost: 1200,
  costInTwd: 1200,
})

const days: ScheduleDay[] = [
  { date: '2026-12-01', activities: [sensoji, sushi] },
  { date: '2026-12-02', activities: [] },
  { date: '2026-12-03', activities: [tower] },
]

function names(groups: TableGroup[]) {
  return groups.map((group) => group.rows.map((row) => row.activity.name))
}

describe('parseView', () => {
  it.each([
    ['timeline', 'timeline'],
    ['board', 'board'],
    ['table', 'table'],
    ['map', null],
    [undefined, null],
  ])('reads %s as %s', (value, view) => {
    expect(parseView(value)).toBe(view)
  })
})

describe('filterDays', () => {
  it('returns the days unchanged without filters', () => {
    expect(filterDays(days, { category: null, bookingStatus: null })).toBe(days)
  })

  it('keeps every day but only the matching activities', () => {
    const result = filterDays(days, { category: 'Sightseeing', bookingStatus: null })

    expect(result.map((day) => day.activities.map((a) => a.name))).toEqual([
      ['Senso-ji'],
      [],
      ['Tokyo Tower'],
    ])
  })

  it('combines category and booking status', () => {
    const result = filterDays(days, { category: 'Sightseeing', bookingStatus: 'Paid' })

    expect(result.flatMap((day) => day.activities.map((a) => a.name))).toEqual(['Tokyo Tower'])
  })
})

describe('nextSort', () => {
  it('toggles the direction of the same column', () => {
    expect(nextSort({ key: 'name', direction: 'asc' }, 'name')).toEqual({
      key: 'name',
      direction: 'desc',
    })
  })

  it('starts a new column ascending, except cost which starts with the highest', () => {
    expect(nextSort(DEFAULT_SORT, 'name')).toEqual({ key: 'name', direction: 'asc' })
    expect(nextSort(DEFAULT_SORT, 'cost')).toEqual({ key: 'cost', direction: 'desc' })
  })
})

describe('tableGroups', () => {
  it('groups by day in time order with subtotals, skipping empty days', () => {
    const groups = tableGroups(days, DEFAULT_SORT)

    expect(groups.map((group) => [group.date, group.dayNumber])).toEqual([
      ['2026-12-01', 1],
      ['2026-12-03', 3],
    ])
    expect(names(groups)).toEqual([['Sushi Dai', 'Senso-ji'], ['Tokyo Tower']])
    expect(groups.map((group) => group.totalInTwd)).toEqual([630, 1200])
  })

  it('reverses days and times when sorting by date descending', () => {
    const groups = tableGroups(days, { key: 'date', direction: 'desc' })

    expect(names(groups)).toEqual([['Tokyo Tower'], ['Senso-ji', 'Sushi Dai']])
  })

  it('lists every activity in one group when sorting by another column', () => {
    const groups = tableGroups(days, { key: 'cost', direction: 'desc' })

    expect(groups).toHaveLength(1)
    expect(groups[0]!.date).toBeNull()
    expect(names(groups)).toEqual([['Tokyo Tower', 'Sushi Dai', 'Senso-ji']])
    expect(groups[0]!.rows.map((row) => row.dayNumber)).toEqual([3, 1, 1])
    expect(groups[0]!.totalInTwd).toBe(1830)
  })

  it.each([
    ['name', 'asc', ['Senso-ji', 'Sushi Dai', 'Tokyo Tower']],
    ['name', 'desc', ['Tokyo Tower', 'Sushi Dai', 'Senso-ji']],
    ['category', 'asc', ['Senso-ji', 'Tokyo Tower', 'Sushi Dai']],
    ['booking', 'asc', ['Senso-ji', 'Sushi Dai', 'Tokyo Tower']],
  ] as const)('sorts by %s %s', (key, direction, expected) => {
    expect(names(tableGroups(days, { key, direction }))).toEqual([expected])
  })

  it('leaves the subtotal empty when a cost cannot be converted', () => {
    const unconverted = { ...sushi, costInTwd: null }
    const groups = tableGroups([{ date: '2026-12-01', activities: [unconverted] }], DEFAULT_SORT)

    expect(groups[0]!.totalInTwd).toBeNull()
  })

  it('returns nothing for a trip without activities', () => {
    expect(
      tableGroups([{ date: '2026-12-01', activities: [] }], { key: 'name', direction: 'asc' }),
    ).toEqual([])
  })
})

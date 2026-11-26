import type { Activity, ActivityCategory, BookingStatus, ScheduleDay } from '@/types/activity'
import { CATEGORIES, BOOKING_STATUSES } from '@/utils/activityDetails'
import { totalInTwd } from '@/utils/money'
import { toMinutes } from '@/utils/board'

export type ScheduleView = 'timeline' | 'board' | 'table'

export const SCHEDULE_VIEWS: ScheduleView[] = ['timeline', 'board', 'table']

export function parseView(value: unknown): ScheduleView | null {
  return SCHEDULE_VIEWS.includes(value as ScheduleView) ? (value as ScheduleView) : null
}

export interface ActivityFilters {
  category: ActivityCategory | null
  bookingStatus: BookingStatus | null
}

export const NO_FILTERS: ActivityFilters = { category: null, bookingStatus: null }

export function matchesFilters(activity: Activity, filters: ActivityFilters): boolean {
  return (
    (filters.category === null || activity.category === filters.category) &&
    (filters.bookingStatus === null || activity.bookingStatus === filters.bookingStatus)
  )
}

export function filterDays(days: ScheduleDay[], filters: ActivityFilters): ScheduleDay[] {
  if (filters.category === null && filters.bookingStatus === null) {
    return days
  }

  return days.map((day) => ({
    ...day,
    activities: day.activities.filter((activity) => matchesFilters(activity, filters)),
  }))
}

export type SortKey = 'date' | 'name' | 'category' | 'booking' | 'cost'
export type SortDirection = 'asc' | 'desc'

export interface TableSort {
  key: SortKey
  direction: SortDirection
}

export const DEFAULT_SORT: TableSort = { key: 'date', direction: 'asc' }

export function nextSort(current: TableSort, key: SortKey): TableSort {
  if (current.key !== key) {
    return { key, direction: key === 'cost' ? 'desc' : 'asc' }
  }

  return { key, direction: current.direction === 'asc' ? 'desc' : 'asc' }
}

export interface TableRow {
  activity: Activity
  dayNumber: number
}

export interface TableGroup {
  date: string | null
  dayNumber: number | null
  rows: TableRow[]
  totalInTwd: number | null
}

function chronological(a: TableRow, b: TableRow): number {
  return (
    a.activity.date.localeCompare(b.activity.date) ||
    toMinutes(a.activity.startTime) - toMinutes(b.activity.startTime)
  )
}

const categoryOrder = new Map(CATEGORIES.map((category, index) => [category.value, index]))
const bookingOrder = new Map(BOOKING_STATUSES.map((status, index) => [status.value, index]))

function compareBy(key: SortKey): (a: TableRow, b: TableRow) => number {
  switch (key) {
    case 'name':
      return (a, b) => a.activity.name.localeCompare(b.activity.name)
    case 'category':
      return (a, b) =>
        (categoryOrder.get(a.activity.category) ?? 0) -
        (categoryOrder.get(b.activity.category) ?? 0)
    case 'booking':
      return (a, b) =>
        (bookingOrder.get(a.activity.bookingStatus) ?? 0) -
        (bookingOrder.get(b.activity.bookingStatus) ?? 0)
    case 'cost':
      return (a, b) => (a.activity.costInTwd ?? -1) - (b.activity.costInTwd ?? -1)
    case 'date':
      return () => 0
  }
}

function sortRows(rows: TableRow[], sort: TableSort): TableRow[] {
  const sign = sort.direction === 'asc' ? 1 : -1
  const compare = compareBy(sort.key)
  return [...rows].sort((a, b) => sign * (compare(a, b) || chronological(a, b)))
}

function groupOf(date: string | null, dayNumber: number | null, rows: TableRow[]): TableGroup {
  return {
    date,
    dayNumber,
    rows,
    totalInTwd: totalInTwd(rows.map((row) => row.activity)),
  }
}

export function tableGroups(days: ScheduleDay[], sort: TableSort): TableGroup[] {
  const rows = days.flatMap((day, index) =>
    day.activities.map((activity) => ({ activity, dayNumber: index + 1 })),
  )

  if (sort.key !== 'date') {
    return rows.length > 0 ? [groupOf(null, null, sortRows(rows, sort))] : []
  }

  const groups = days
    .map((day, index) =>
      groupOf(
        day.date,
        index + 1,
        sortRows(
          rows.filter((row) => row.dayNumber === index + 1),
          { key: 'date', direction: 'asc' },
        ),
      ),
    )
    .filter((group) => group.rows.length > 0)

  return sort.direction === 'asc'
    ? groups
    : groups.reverse().map((group) => ({ ...group, rows: [...group.rows].reverse() }))
}

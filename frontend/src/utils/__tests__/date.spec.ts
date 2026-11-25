import { describe, expect, it } from 'vitest'
import { countDays, dateParts, formatDateRange, formatDayLabel } from '../date'

describe('formatDateRange', () => {
  it('shows a single date for a one-day trip', () => {
    expect(formatDateRange('2026-12-01', '2026-12-01')).toBe('Dec 1, 2026')
  })

  it('shows the month once when both dates are in the same month', () => {
    expect(formatDateRange('2026-12-01', '2026-12-05')).toBe('Dec 1 – 5, 2026')
  })

  it('shows both months when the trip crosses months', () => {
    expect(formatDateRange('2026-11-28', '2026-12-02')).toBe('Nov 28 – Dec 2, 2026')
  })

  it('shows both years when the trip crosses years', () => {
    expect(formatDateRange('2026-12-30', '2027-01-02')).toBe('Dec 30, 2026 – Jan 2, 2027')
  })
})

describe('countDays', () => {
  it('counts both the start and the end date', () => {
    expect(countDays('2026-12-01', '2026-12-05')).toBe(5)
  })

  it('counts a one-day trip as one day', () => {
    expect(countDays('2026-12-01', '2026-12-01')).toBe(1)
  })

  it('counts across months and years', () => {
    expect(countDays('2026-12-30', '2027-01-02')).toBe(4)
  })
})

describe('formatDayLabel', () => {
  it.each([
    ['2026-12-01', 'Tue, Dec 1'],
    ['2027-01-02', 'Sat, Jan 2'],
  ])('formats %s as %s', (date, expected) => {
    expect(formatDayLabel(date)).toBe(expected)
  })
})

describe('dateParts', () => {
  it('splits a date into month, day and weekday', () => {
    expect(dateParts('2027-01-02')).toEqual({ month: 'Jan', day: 2, weekday: 'Sat' })
  })
})

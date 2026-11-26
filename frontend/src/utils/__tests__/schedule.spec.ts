import { describe, expect, it } from 'vitest'
import type { Activity, ScheduleDay } from '@/types/activity'
import { formatTime, slotAfter, withActivity, withoutActivity } from '../schedule'

function activity(id: string, date: string, startTime: string, endTime: string): Activity {
  return { id, name: id, date, startTime, endTime, cost: 0, currency: 'TWD', costInTwd: 0 }
}

const tsukiji = activity('tsukiji', '2026-12-01', '07:00:00', '08:00:00')
const shibuya = activity('shibuya', '2026-12-01', '15:00:00', '16:00:00')

function days(): ScheduleDay[] {
  return [
    { date: '2026-12-01', activities: [tsukiji, shibuya] },
    { date: '2026-12-02', activities: [] },
  ]
}

function names(scheduleDays: ScheduleDay[]): string[][] {
  return scheduleDays.map((day) => day.activities.map((item) => item.name))
}

describe('formatTime', () => {
  it.each([
    ['10:00:00', '10:00'],
    ['09:05', '09:05'],
  ])('formats %s as %s', (value, expected) => {
    expect(formatTime(value)).toBe(expected)
  })
})

describe('withActivity', () => {
  it('inserts a new activity in start time order', () => {
    const sensoji = activity('senso-ji', '2026-12-01', '09:00', '10:00')

    expect(names(withActivity(days(), sensoji))).toEqual([['tsukiji', 'senso-ji', 'shibuya'], []])
  })

  it('compares times with and without seconds', () => {
    const early = activity('early', '2026-12-01', '06:30', '06:45')

    expect(names(withActivity(days(), early))[0]).toEqual(['early', 'tsukiji', 'shibuya'])
  })

  it('moves an updated activity to its new day', () => {
    const moved = { ...tsukiji, date: '2026-12-02' }

    expect(names(withActivity(days(), moved))).toEqual([['shibuya'], ['tsukiji']])
  })

  it('re-sorts an updated activity within the same day', () => {
    const later = { ...tsukiji, startTime: '17:00', endTime: '18:00' }

    expect(names(withActivity(days(), later))[0]).toEqual(['shibuya', 'tsukiji'])
  })

  it('does not change the original days', () => {
    const original = days()

    withActivity(original, activity('senso-ji', '2026-12-01', '09:00', '10:00'))

    expect(names(original)).toEqual([['tsukiji', 'shibuya'], []])
  })
})

describe('withoutActivity', () => {
  it('removes the activity from its day', () => {
    expect(names(withoutActivity(days(), 'tsukiji'))).toEqual([['shibuya'], []])
  })

  it('keeps every day even when it becomes empty', () => {
    const result = withoutActivity(withoutActivity(days(), 'tsukiji'), 'shibuya')

    expect(result.map((day) => day.date)).toEqual(['2026-12-01', '2026-12-02'])
  })
})

describe('slotAfter', () => {
  it.each([
    ['11:30:00', 90, '11:30', '13:00'],
    ['11:32', 60, '11:35', '12:35'],
    ['09:00', 15, '09:00', '09:15'],
    ['22:30', 120, '22:30', '23:55'],
    ['23:58', 60, '23:50', '23:55'],
    ['10:00', 0, '10:00', '10:05'],
  ])('after %s for %d minutes is %s – %s', (end, minutes, startTime, endTime) => {
    expect(slotAfter(end, minutes)).toEqual({ startTime, endTime })
  })
})

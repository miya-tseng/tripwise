import { describe, expect, it } from 'vitest'
import type { Activity } from '@/types/activity'
import {
  durationOf,
  fromMinutes,
  insertionIndex,
  shiftedStartTime,
  startTimeForDrop,
  toMinutes,
} from '../board'

function activity(id: string, startTime: string, endTime: string): Activity {
  return {
    id,
    name: id,
    date: '2026-12-01',
    startTime,
    endTime,
    cost: 0,
    currency: 'TWD',
    costInTwd: 0,
    category: 'Other',
    bookingStatus: 'NotNeeded',
    notes: '',
    confirmationCode: '',
    link: null,
  }
}

const morning = activity('morning', '09:00:00', '10:30:00')
const lunch = activity('lunch', '12:02', '13:00')
const dragged = activity('dragged', '15:00', '16:30')

describe('time helpers', () => {
  it('converts times to minutes and back', () => {
    expect(toMinutes('10:30:00')).toBe(630)
    expect(fromMinutes(630)).toBe('10:30')
  })

  it('keeps converted times within the day', () => {
    expect(fromMinutes(-30)).toBe('00:00')
    expect(fromMinutes(24 * 60 + 10)).toBe('23:55')
  })

  it('measures the duration of an activity', () => {
    expect(durationOf(dragged)).toBe(90)
  })
})

describe('startTimeForDrop', () => {
  it('starts right after the card above', () => {
    expect(startTimeForDrop([morning, lunch], 1, dragged)).toBe('10:30')
  })

  it('rounds up to five minutes after the card above', () => {
    expect(startTimeForDrop([morning, lunch], 2, dragged)).toBe('13:00')
    expect(startTimeForDrop([lunch], 1, activity('x', '08:00', '08:30'))).toBe('13:00')
    expect(startTimeForDrop([activity('odd', '09:00', '10:02')], 1, dragged)).toBe('10:05')
  })

  it('ends before the first card when dropped at the top', () => {
    expect(startTimeForDrop([morning, lunch], 0, dragged)).toBe('07:30')
  })

  it('rounds down to five minutes when dropped before an odd start', () => {
    expect(startTimeForDrop([lunch], 0, dragged)).toBe('10:30')
  })

  it('never starts before midnight', () => {
    expect(startTimeForDrop([activity('early', '00:30', '01:00')], 0, dragged)).toBe('00:00')
  })

  it('keeps the start time on an empty day', () => {
    expect(startTimeForDrop([], 0, dragged)).toBe('15:00')
  })

  it('ignores the dragged card itself when finding neighbours', () => {
    expect(startTimeForDrop([morning, dragged, lunch], 2, dragged)).toBe('13:00')
    expect(startTimeForDrop([dragged], 1, dragged)).toBe('15:00')
  })
})

describe('shiftedStartTime', () => {
  it('moves the start time by the given minutes', () => {
    expect(shiftedStartTime(morning, 30)).toBe('09:30')
    expect(shiftedStartTime(morning, -30)).toBe('08:30')
  })
})

describe('insertionIndex', () => {
  it('finds the slot above the first card whose middle is below the pointer', () => {
    expect(insertionIndex([100, 200, 300], 50)).toBe(0)
    expect(insertionIndex([100, 200, 300], 150)).toBe(1)
    expect(insertionIndex([100, 200, 300], 350)).toBe(3)
    expect(insertionIndex([], 10)).toBe(0)
  })
})

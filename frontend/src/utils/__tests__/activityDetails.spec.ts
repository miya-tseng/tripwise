import { describe, expect, it } from 'vitest'
import {
  BOOKING_STATUSES,
  bookingStatusMeta,
  CATEGORIES,
  categoryMeta,
  linkError,
  notesPreview,
} from '../activityDetails'

describe('activity details helpers', () => {
  it('describes every category with an icon', () => {
    expect(CATEGORIES.map((category) => category.value)).toEqual([
      'Sightseeing',
      'Food',
      'Transport',
      'Shopping',
      'Experience',
      'Other',
    ])
    expect(categoryMeta('Food')).toEqual({ value: 'Food', label: 'Food', icon: '🍜' })
  })

  it('gives booking statuses a tone', () => {
    expect(BOOKING_STATUSES).toHaveLength(4)
    expect(bookingStatusMeta('ToBook')).toMatchObject({ label: 'To book', tone: 'warning' })
    expect(bookingStatusMeta('Paid')).toMatchObject({ label: 'Paid', tone: 'success' })
  })

  it.each([
    ['', ''],
    ['  ', ''],
    ['https://example.com/tickets', ''],
    ['http://example.com', 'Link must start with https://'],
    ['example.com', 'Link must start with https://'],
  ])('checks the link %j', (link, error) => {
    expect(linkError(link)).toBe(error)
  })

  it('previews the first line of notes', () => {
    expect(notesPreview('Bring passport\nArrive early')).toBe('Bring passport')
    expect(notesPreview('a'.repeat(100), 10)).toBe('aaaaaaaaaa…')
    expect(notesPreview('')).toBe('')
  })
})

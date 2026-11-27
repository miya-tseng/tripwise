import type { ActivityCategory, ActivityDetails, BookingStatus } from '@/types/activity'

export interface CategoryMeta {
  value: ActivityCategory
  label: string
  icon: string
}

export interface BookingStatusMeta {
  value: BookingStatus
  label: string
  tone: 'muted' | 'warning' | 'success'
}

export const CATEGORIES: CategoryMeta[] = [
  { value: 'Sightseeing', label: 'Sightseeing', icon: '🏯' },
  { value: 'Food', label: 'Food', icon: '🍜' },
  { value: 'Transport', label: 'Transport', icon: '🚆' },
  { value: 'Shopping', label: 'Shopping', icon: '🛍️' },
  { value: 'Experience', label: 'Experience', icon: '🎟️' },
  { value: 'Other', label: 'Other', icon: '📌' },
]

export const BOOKING_STATUSES: BookingStatusMeta[] = [
  { value: 'NotNeeded', label: 'No booking needed', tone: 'muted' },
  { value: 'ToBook', label: 'To book', tone: 'warning' },
  { value: 'Booked', label: 'Booked', tone: 'success' },
  { value: 'Paid', label: 'Paid', tone: 'success' },
]

export const BOOKING_OPTIONS = BOOKING_STATUSES.map((status) => ({
  value: status.value,
  label: status.label,
}))

export const DEFAULT_DETAILS: ActivityDetails = {
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

export function categoryMeta(category: ActivityCategory): CategoryMeta {
  return CATEGORIES.find((item) => item.value === category) ?? CATEGORIES[CATEGORIES.length - 1]!
}

export function bookingStatusMeta(status: BookingStatus): BookingStatusMeta {
  return BOOKING_STATUSES.find((item) => item.value === status) ?? BOOKING_STATUSES[0]!
}

export function linkError(link: string): string {
  const trimmed = link.trim()

  if (trimmed === '') {
    return ''
  }

  try {
    return new URL(trimmed).protocol === 'https:' ? '' : 'Link must start with https://'
  } catch {
    return 'Link must start with https://'
  }
}

export function notesPreview(notes: string, maxLength = 80): string {
  const firstLine = notes.split('\n')[0]?.trim() ?? ''
  return firstLine.length > maxLength ? `${firstLine.slice(0, maxLength).trimEnd()}…` : firstLine
}

import type { BookingStatus } from './activity'

export type ChecklistSection = 'ToBook' | 'ToPack' | 'Other'

export interface ChecklistItem {
  id: string
  text: string
  section: ChecklistSection
  done: boolean
  dueDate: string | null
  position: number
}

export interface BookingItem {
  kind: 'Activity' | 'Stay'
  id: string
  name: string
  date: string
  bookingStatus: BookingStatus
  done: boolean
}

export interface Checklist {
  tripId: string
  done: number
  total: number
  items: ChecklistItem[]
  bookings: BookingItem[]
}

export interface SaveChecklistItemInput {
  text: string
  section: ChecklistSection
  dueDate: string | null
}

export const MAX_CHECKLIST_TEXT_LENGTH = 200

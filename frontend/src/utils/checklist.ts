import type { ChecklistItem, ChecklistSection } from '@/types/checklist'
import { countDays, dateParts } from '@/utils/date'

export interface SectionMeta {
  value: ChecklistSection
  label: string
  icon: string
  placeholder: string
}

export const SECTIONS: SectionMeta[] = [
  { value: 'ToBook', label: 'To book', icon: '🎟️', placeholder: 'e.g. Travel insurance' },
  { value: 'ToPack', label: 'To pack', icon: '🧳', placeholder: 'e.g. Passport' },
  { value: 'Other', label: 'Other', icon: '📝', placeholder: 'e.g. Exchange some yen' },
]

export const PACKING_SUGGESTIONS = [
  'Passport',
  'Phone charger',
  'Power adapter',
  'Medicine',
  'Toiletries',
  'Cash and cards',
  'Umbrella',
  'Comfortable shoes',
]

export function itemsIn(items: ChecklistItem[], section: ChecklistSection): ChecklistItem[] {
  return items.filter((item) => item.section === section).sort((a, b) => a.position - b.position)
}

export function isOverdue(item: Pick<ChecklistItem, 'done' | 'dueDate'>, today: string): boolean {
  return !item.done && item.dueDate !== null && item.dueDate < today
}

export function dueLabel(dueDate: string, today: string): string {
  const days = countDays(today, dueDate) - 1
  const { month, day } = dateParts(dueDate)

  if (days < 0) {
    return `Overdue · ${month} ${day}`
  }

  if (days === 0) {
    return 'Due today'
  }

  if (days === 1) {
    return 'Due tomorrow'
  }

  return days < 7 ? `Due in ${days} days` : `Due ${month} ${day}`
}

export function missingSuggestions(items: ChecklistItem[], limit = 6): string[] {
  const existing = new Set(items.map((item) => item.text.trim().toLowerCase()))
  return PACKING_SUGGESTIONS.filter((text) => !existing.has(text.toLowerCase())).slice(0, limit)
}

export function moveId(ids: string[], id: string, toIndex: number): string[] {
  const from = ids.indexOf(id)

  if (from < 0) {
    return ids
  }

  const result = ids.filter((other) => other !== id)
  const clamped = Math.min(Math.max(toIndex, 0), result.length)
  result.splice(clamped, 0, id)
  return result
}

export function progressPercent(done: number, total: number): number {
  return total > 0 ? Math.round((done / total) * 100) : 0
}

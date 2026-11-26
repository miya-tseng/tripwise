import { describe, expect, it } from 'vitest'
import type { ChecklistItem } from '@/types/checklist'
import {
  dueLabel,
  isOverdue,
  itemsIn,
  missingSuggestions,
  moveId,
  progressPercent,
} from '../checklist'

function item(text: string, overrides: Partial<ChecklistItem> = {}): ChecklistItem {
  return {
    id: `${text}-id`,
    text,
    section: 'ToPack',
    done: false,
    dueDate: null,
    position: 0,
    ...overrides,
  }
}

describe('itemsIn', () => {
  it('keeps one section in position order', () => {
    const items = [
      item('Charger', { position: 1 }),
      item('Insurance', { section: 'ToBook' }),
      item('Passport', { position: 0 }),
    ]

    expect(itemsIn(items, 'ToPack').map((entry) => entry.text)).toEqual(['Passport', 'Charger'])
  })
})

describe('isOverdue', () => {
  it('is overdue only when open and past its due date', () => {
    expect(isOverdue(item('A', { dueDate: '2026-11-20' }), '2026-11-26')).toBe(true)
    expect(isOverdue(item('A', { dueDate: '2026-11-26' }), '2026-11-26')).toBe(false)
    expect(isOverdue(item('A', { dueDate: '2026-11-20', done: true }), '2026-11-26')).toBe(false)
    expect(isOverdue(item('A'), '2026-11-26')).toBe(false)
  })
})

describe('dueLabel', () => {
  const today = '2026-11-26'

  it.each([
    ['2026-11-20', 'Overdue · Nov 20'],
    ['2026-11-26', 'Due today'],
    ['2026-11-27', 'Due tomorrow'],
    ['2026-11-30', 'Due in 4 days'],
    ['2026-12-10', 'Due Dec 10'],
  ])('describes %s as "%s"', (due, label) => {
    expect(dueLabel(due, today)).toBe(label)
  })
})

describe('missingSuggestions', () => {
  it('skips what is already on the list, ignoring case', () => {
    const suggestions = missingSuggestions([item('passport'), item(' Umbrella ')], 3)

    expect(suggestions).toEqual(['Phone charger', 'Power adapter', 'Medicine'])
  })
})

describe('moveId', () => {
  it('moves an id to a new index', () => {
    expect(moveId(['a', 'b', 'c'], 'c', 0)).toEqual(['c', 'a', 'b'])
    expect(moveId(['a', 'b', 'c'], 'a', 5)).toEqual(['b', 'c', 'a'])
    expect(moveId(['a', 'b', 'c'], 'b', -1)).toEqual(['b', 'a', 'c'])
    expect(moveId(['a', 'b'], 'x', 0)).toEqual(['a', 'b'])
  })
})

describe('progressPercent', () => {
  it('rounds and handles an empty list', () => {
    expect(progressPercent(1, 3)).toBe(33)
    expect(progressPercent(0, 0)).toBe(0)
  })
})

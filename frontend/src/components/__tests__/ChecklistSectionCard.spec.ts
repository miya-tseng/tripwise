import { afterEach, describe, expect, it } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import ChecklistSectionCard from '../ChecklistSectionCard.vue'
import type { BookingItem, ChecklistItem } from '@/types/checklist'
import { SECTIONS } from '@/utils/checklist'

const toBook = SECTIONS[0]!
const toPack = SECTIONS[1]!

function item(id: string, text: string, overrides: Partial<ChecklistItem> = {}): ChecklistItem {
  return { id, text, section: 'ToPack', done: false, dueDate: null, position: 0, ...overrides }
}

const passport = item('a', 'Passport', { done: true })
const charger = item('b', 'Charger', { position: 1, dueDate: '2026-11-20' })
const adapter = item('c', 'Adapter', { position: 2 })

const teamLab: BookingItem = {
  kind: 'Activity',
  id: 'act',
  name: 'teamLab',
  date: '2026-12-02',
  bookingStatus: 'ToBook',
  done: false,
}

let wrapper: VueWrapper | null = null

function mountCard(props: Record<string, unknown> = {}) {
  wrapper = mount(ChecklistSectionCard, {
    props: { section: toPack, items: [passport, charger, adapter], today: '2026-11-26', ...props },
    attachTo: document.body,
  })
  return wrapper
}

describe('ChecklistSectionCard', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('shows the items, their state and the section progress', () => {
    const card = mountCard()
    const rows = card.findAll('[data-testid="checklist-item"]')

    expect(card.find('h2').text()).toBe('🧳 To pack')
    expect(card.find('[data-testid="section-count"]').text()).toBe('1 / 3')
    expect(rows.map((row) => row.find('[data-testid="item-text"]').text())).toEqual([
      'Passport',
      'Charger',
      'Adapter',
    ])
    expect(rows[0]!.classes()).toContain('done')
    expect(rows[1]!.classes()).toContain('overdue')
    expect(rows[1]!.find('[data-testid="item-due"]').text()).toBe('Overdue · Nov 20')
  })

  it('adds an item from the quick add box', async () => {
    const card = mountCard()
    const input = card.find('[data-testid="quick-add"]')

    await input.setValue('  Socks ')
    await card.find('.quick-add').trigger('submit')

    expect(card.emitted('add')).toEqual([['Socks']])
    expect((input.element as HTMLInputElement).value).toBe('')
  })

  it('ignores an empty quick add', async () => {
    const card = mountCard()

    await card.find('[data-testid="quick-add"]').setValue('   ')
    await card.find('.quick-add').trigger('submit')

    expect(card.emitted('add')).toBeUndefined()
    expect(card.find('[data-testid="quick-add-button"]').attributes('disabled')).toBeDefined()
  })

  it('ticks and unticks an item', async () => {
    const card = mountCard()
    const boxes = card.findAll('[data-testid="item-checkbox"]')

    await boxes[1]!.setValue(true)
    await boxes[0]!.setValue(false)

    expect(card.emitted('toggle')).toEqual([
      ['b', true],
      ['a', false],
    ])
  })

  it('edits the text, section and due date of an item', async () => {
    const card = mountCard()

    await card.findAll('[data-testid="edit-item"]')[2]!.trigger('click')
    await flushPromises()
    expect(document.activeElement).toBe(card.find('[data-testid="edit-item-text"]').element)

    await card.find('[data-testid="edit-item-text"]').setValue('Power adapter')
    await card.find('[data-testid="edit-item-due"]').setValue('2026-12-01')
    await card.find('[data-testid="edit-item-form"]').trigger('submit')

    expect(card.emitted('update')).toEqual([
      ['c', { text: 'Power adapter', section: 'ToPack', dueDate: '2026-12-01' }],
    ])
    expect(card.find('[data-testid="edit-item-form"]').exists()).toBe(false)
  })

  it('clears the due date when it is emptied', async () => {
    const card = mountCard()

    await card.findAll('[data-testid="edit-item"]')[1]!.trigger('click')
    await card.find('[data-testid="edit-item-due"]').setValue('')
    await card.find('[data-testid="edit-item-form"]').trigger('submit')

    expect(card.emitted('update')![0]![1]).toMatchObject({ dueDate: null })
  })

  it('deletes an item from its edit form', async () => {
    const card = mountCard()

    await card.findAll('[data-testid="edit-item"]')[0]!.trigger('click')
    await card.find('[data-testid="delete-item"]').trigger('click')

    expect(card.emitted('remove')).toEqual([['a']])
  })

  it('moves an item with the arrow keys on its handle', async () => {
    const card = mountCard()
    const handles = card.findAll('[data-testid="drag-handle"]')

    await handles[2]!.trigger('keydown', { key: 'ArrowUp' })
    await handles[0]!.trigger('keydown', { key: 'ArrowUp' })

    expect(card.emitted('reorder')).toEqual([[['a', 'c', 'b']]])
  })

  it('reorders by dragging an item onto another', async () => {
    const card = mountCard()
    const rows = card.findAll('[data-testid="checklist-item"]')
    const dataTransfer = { setData: () => {}, effectAllowed: '' }

    await rows[2]!.trigger('dragstart', { dataTransfer })
    await rows[0]!.trigger('dragover', { clientY: -1000 })
    await rows[0]!.trigger('drop')

    expect(card.emitted('reorder')).toEqual([[['c', 'a', 'b']]])
  })

  it('lists bookings from the plan and ticks them', async () => {
    const card = mountCard({ section: toBook, items: [], bookings: [teamLab] })
    const booking = card.find('[data-testid="booking-item"]')

    expect(booking.find('[data-testid="booking-name"]').text()).toBe('🔗 teamLab')
    expect(booking.find('[data-testid="booking-item-status"]').text()).toBe('To book')
    expect(card.find('[data-testid="section-count"]').text()).toBe('0 / 1')

    await booking.find('[data-testid="booking-checkbox"]').setValue(true)

    expect(card.emitted('toggleBooking')).toEqual([[teamLab, true]])
  })

  it('adds a suggestion once and hides it', async () => {
    const card = mountCard({ suggestions: ['Medicine', 'Umbrella'] })
    const medicine = card.findAll('[data-testid="suggestion"]')[0]!

    await medicine.trigger('click')

    expect(card.emitted('add')).toEqual([['Medicine']])
    expect(card.findAll('[data-testid="suggestion"]').map((chip) => chip.text())).toEqual([
      '+ Umbrella',
    ])
  })
})

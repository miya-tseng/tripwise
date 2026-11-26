import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import {
  addChecklistItem,
  fetchChecklist,
  reorderChecklist,
  setBookingDone,
  setChecklistItemDone,
} from '@/api/checklist'
import type { Checklist } from '@/types/checklist'
import { useChecklistStore } from '../checklist'

vi.mock('@/api/checklist')

const checklist: Checklist = {
  tripId: 'tokyo-id',
  done: 0,
  total: 2,
  items: [
    { id: 'a', text: 'Passport', section: 'ToPack', done: false, dueDate: null, position: 0 },
    { id: 'b', text: 'Charger', section: 'ToPack', done: false, dueDate: null, position: 1 },
  ],
  bookings: [],
}

describe('checklist store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  it('loads the checklist of the trip', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    const store = useChecklistStore()

    await store.load('tokyo-id')

    expect(fetchChecklist).toHaveBeenCalledWith('tokyo-id')
    expect(store.checklist).toEqual(checklist)
    expect(store.loading).toBe(false)
  })

  it('keeps the load error', async () => {
    vi.mocked(fetchChecklist).mockRejectedValue(new Error('Trip not found.'))
    const store = useChecklistStore()

    await store.load('tokyo-id')

    expect(store.loadError).toBe('Trip not found.')
    expect(store.checklist).toBeNull()
  })

  it('replaces the checklist with the server answer after a change', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    const added = { ...checklist, total: 3 }
    vi.mocked(addChecklistItem).mockResolvedValue(added)
    const store = useChecklistStore()
    await store.load('tokyo-id')

    const saved = await store.add({ text: 'Adapter', section: 'ToPack', dueDate: null })

    expect(saved).toBe(true)
    expect(addChecklistItem).toHaveBeenCalledWith('tokyo-id', {
      text: 'Adapter',
      section: 'ToPack',
      dueDate: null,
    })
    expect(store.checklist).toEqual(added)
  })

  it('ticks right away and rolls back when the server refuses', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    let reject: (reason: Error) => void = () => {}
    vi.mocked(setChecklistItemDone).mockReturnValue(
      new Promise((_, fail) => {
        reject = fail
      }),
    )
    const store = useChecklistStore()
    await store.load('tokyo-id')

    const pending = store.toggle('a', true)
    expect(store.checklist!.items[0]!.done).toBe(true)

    reject(new Error('Checklist item was not found.'))
    expect(await pending).toBe(false)
    expect(store.checklist!.items[0]!.done).toBe(false)
    expect(store.error).toBe('Checklist item was not found.')
  })

  it('reorders right away', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    vi.mocked(reorderChecklist).mockReturnValue(new Promise(() => {}))
    const store = useChecklistStore()
    await store.load('tokyo-id')

    void store.reorder('ToPack', ['b', 'a'])

    expect(reorderChecklist).toHaveBeenCalledWith('tokyo-id', 'ToPack', ['b', 'a'])
    expect(store.checklist!.items.map((item) => [item.id, item.position])).toEqual([
      ['a', 1],
      ['b', 0],
    ])
  })

  it('ticks a booking through the checklist', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    vi.mocked(setBookingDone).mockResolvedValue(checklist)
    const store = useChecklistStore()
    await store.load('tokyo-id')
    const booking = {
      kind: 'Activity' as const,
      id: 'act',
      name: 'teamLab',
      date: '2026-12-01',
      bookingStatus: 'ToBook' as const,
      done: false,
    }

    await store.toggleBooking(booking, true)

    expect(setBookingDone).toHaveBeenCalledWith('tokyo-id', booking, true)
  })
})

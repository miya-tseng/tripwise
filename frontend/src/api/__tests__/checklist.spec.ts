import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addChecklistItem,
  deleteChecklistItem,
  fetchChecklist,
  reorderChecklist,
  setBookingDone,
  setChecklistItemDone,
  updateChecklistItem,
} from '../checklist'

describe('checklist api', () => {
  const fetchMock = vi.fn<typeof fetch>()
  const base = '/api/trips/tokyo-id/checklist'

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockImplementation(
      async () =>
        new Response('{"tripId":"tokyo-id"}', { headers: { 'Content-Type': 'application/json' } }),
    )
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  function lastCall() {
    const [url, init] = fetchMock.mock.lastCall!
    return {
      url,
      method: init?.method ?? 'GET',
      body: init?.body ? JSON.parse(String(init.body)) : null,
    }
  }

  it('fetches the checklist', async () => {
    const checklist = await fetchChecklist('tokyo-id')

    expect(lastCall()).toEqual({ url: base, method: 'GET', body: null })
    expect(checklist.tripId).toBe('tokyo-id')
  })

  it('adds and updates items', async () => {
    const input = { text: 'Passport', section: 'ToPack' as const, dueDate: null }

    await addChecklistItem('tokyo-id', input)
    expect(lastCall()).toEqual({ url: base, method: 'POST', body: input })

    await updateChecklistItem('tokyo-id', 'item-1', input)
    expect(lastCall()).toEqual({ url: `${base}/item-1`, method: 'PUT', body: input })
  })

  it('ticks, deletes and reorders items', async () => {
    await setChecklistItemDone('tokyo-id', 'item-1', true)
    expect(lastCall()).toEqual({ url: `${base}/item-1/done`, method: 'PUT', body: { done: true } })

    await deleteChecklistItem('tokyo-id', 'item-1')
    expect(lastCall()).toEqual({ url: `${base}/item-1`, method: 'DELETE', body: null })

    await reorderChecklist('tokyo-id', 'ToPack', ['b', 'a'])
    expect(lastCall()).toEqual({
      url: `${base}/order`,
      method: 'PUT',
      body: { section: 'ToPack', itemIds: ['b', 'a'] },
    })
  })

  it('ticks bookings of activities and stays', async () => {
    await setBookingDone('tokyo-id', { kind: 'Activity', id: 'act-1' }, true)
    expect(lastCall().url).toBe(`${base}/bookings/activities/act-1`)

    await setBookingDone('tokyo-id', { kind: 'Stay', id: 'stay-1' }, false)
    expect(lastCall()).toEqual({
      url: `${base}/bookings/stays/stay-1`,
      method: 'PUT',
      body: { done: false },
    })
  })
})

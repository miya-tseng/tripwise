import { afterEach, describe, expect, it, vi } from 'vitest'
import { addStay, deleteStay, updateStay } from '../stays'
import type { SaveStayInput } from '@/types/stay'

const input: SaveStayInput = {
  name: 'Hotel Gracery',
  checkIn: '2026-12-01',
  checkOut: '2026-12-02',
  cost: 0,
  currency: 'TWD',
  bookingStatus: 'NotNeeded',
  confirmationCode: '',
}

function mockFetch(status = 200, body: unknown = {}) {
  const fetchMock = vi
    .fn<typeof fetch>()
    .mockResolvedValue(new Response(status === 204 ? null : JSON.stringify(body), { status }))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('stays api', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('posts a new stay', async () => {
    const fetchMock = mockFetch(201, { id: 'hotel' })

    await addStay('trip-1', input)

    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/trips/trip-1/stays')
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(input)
  })

  it('puts an updated stay', async () => {
    const fetchMock = mockFetch(200, { id: 'hotel' })

    await updateStay('trip-1', 'hotel', input)

    expect(fetchMock.mock.calls[0]![0]).toBe('/api/trips/trip-1/stays/hotel')
    expect(fetchMock.mock.calls[0]![1]?.method).toBe('PUT')
  })

  it('deletes a stay', async () => {
    const fetchMock = mockFetch(204)

    await deleteStay('trip-1', 'hotel')

    expect(fetchMock.mock.calls[0]![0]).toBe('/api/trips/trip-1/stays/hotel')
    expect(fetchMock.mock.calls[0]![1]?.method).toBe('DELETE')
  })
})

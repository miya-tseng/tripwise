import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { searchPlaces } from '../places'

describe('places api', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockResolvedValue(
      new Response('[]', { headers: { 'Content-Type': 'application/json' } }),
    )
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('searches with the query, session and location', async () => {
    await searchPlaces('senso ji', 'session-1', { latitude: 35.7, longitude: 139.8 })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/places/autocomplete?query=senso+ji&sessionToken=session-1&lat=35.7&lng=139.8',
    )
  })

  it('asks for destinations only when the kind is destination', async () => {
    await searchPlaces('tok', 'session-1', null, 'destination')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/places/autocomplete?query=tok&sessionToken=session-1&kind=destination',
    )
  })

  it('searches without a location', async () => {
    await searchPlaces('senso', 'session-1', null)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/places/autocomplete?query=senso&sessionToken=session-1',
    )
  })
})

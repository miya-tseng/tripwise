import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fetchOverview } from '../overview'

describe('overview api', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('fetches the overview of the trip', async () => {
    fetchMock.mockResolvedValue(
      new Response('{"tripId":"tokyo-id"}', { headers: { 'Content-Type': 'application/json' } }),
    )

    const overview = await fetchOverview('tokyo-id')

    expect(fetchMock).toHaveBeenCalledWith('/api/trips/tokyo-id/overview')
    expect(overview.tripId).toBe('tokyo-id')
  })
})

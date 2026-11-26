import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, createTrip, deleteTrip, fetchTrips, updateTrip } from '../trips'
import type { Trip } from '@/types/trip'

const tokyo: Trip = {
  id: '01a1f7fd-80f1-7144-8b55-6ffd576d7f76',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-05',
  days: 5,
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('trips api', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  describe('fetchTrips', () => {
    it('throws a readable message when the server cannot be reached', async () => {
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

      await expect(fetchTrips()).rejects.toMatchObject({
        status: 0,
        message: 'Cannot connect to the server.',
      })
    })

    it.each([502, 503, 504])(
      'throws a readable message when the gateway returns %i',
      async (status) => {
        fetchMock.mockResolvedValue(new Response(null, { status }))

        await expect(fetchTrips()).rejects.toMatchObject({
          status,
          message: 'Cannot connect to the server.',
        })
      },
    )

    it('gets all trips', async () => {
      fetchMock.mockResolvedValue(jsonResponse([tokyo]))

      const trips = await fetchTrips()

      expect(fetchMock).toHaveBeenCalledWith('/api/trips')
      expect(trips).toEqual([tokyo])
    })
  })

  describe('createTrip', () => {
    it('posts the input as json and returns the created trip', async () => {
      fetchMock.mockResolvedValue(jsonResponse(tokyo, 201))
      const input = { name: 'Tokyo', startDate: '2026-12-01', endDate: '2026-12-05' }

      const trip = await createTrip(input)

      expect(fetchMock).toHaveBeenCalledWith('/api/trips', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      })
      expect(trip).toEqual(tokyo)
    })

    it('throws the validation messages when the request is invalid', async () => {
      fetchMock.mockResolvedValue(
        jsonResponse({ errors: { Name: ['The Name field is required.'] } }, 400),
      )

      const error = await createTrip({
        name: '',
        startDate: '2026-12-01',
        endDate: '2026-12-01',
      }).catch((e: unknown) => e)

      expect(error).toBeInstanceOf(ApiError)
      expect(error).toMatchObject({ status: 400, message: 'The Name field is required.' })
    })

    it('throws the detail when a business rule is violated', async () => {
      fetchMock.mockResolvedValue(
        jsonResponse(
          { title: 'Business rule violation', detail: 'A trip cannot be longer than 30 days.' },
          400,
        ),
      )

      await expect(
        createTrip({ name: 'Long', startDate: '2026-12-01', endDate: '2027-01-31' }),
      ).rejects.toThrow('A trip cannot be longer than 30 days.')
    })
  })

  describe('updateTrip', () => {
    it('puts the input as json and returns the updated trip', async () => {
      const updated = { ...tokyo, name: 'Osaka', endDate: '2026-12-07', days: 7 }
      fetchMock.mockResolvedValue(jsonResponse(updated))
      const input = { name: 'Osaka', startDate: '2026-12-01', endDate: '2026-12-07' }

      const trip = await updateTrip(tokyo.id, input)

      expect(fetchMock).toHaveBeenCalledWith(`/api/trips/${tokyo.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      })
      expect(trip).toEqual(updated)
    })

    it('throws the conflict detail when activities fall outside the new dates', async () => {
      const detail = '1 activity falls outside the new dates. Move or delete it first.'
      fetchMock.mockResolvedValue(jsonResponse({ title: 'Trip dates conflict', detail }, 409))

      await expect(
        updateTrip(tokyo.id, { name: 'Tokyo', startDate: '2026-12-01', endDate: '2026-12-02' }),
      ).rejects.toMatchObject({ status: 409, message: detail })
    })
  })

  describe('deleteTrip', () => {
    it('sends a delete request for the trip', async () => {
      fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

      await deleteTrip(tokyo.id)

      expect(fetchMock).toHaveBeenCalledWith(`/api/trips/${tokyo.id}`, { method: 'DELETE' })
    })

    it('throws with the status when the trip does not exist', async () => {
      fetchMock.mockResolvedValue(new Response(null, { status: 404 }))

      await expect(deleteTrip(tokyo.id)).rejects.toMatchObject({
        status: 404,
        message: 'Request failed with status 404',
      })
    })
  })
})

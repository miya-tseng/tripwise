import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { createTrip, deleteTrip, fetchTrips } from '@/api/trips'
import type { Trip } from '@/types/trip'
import { useTripsStore } from '../trips'

vi.mock('@/api/trips')

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-05',
  days: 5,
}

const osaka: Trip = {
  id: 'osaka-id',
  name: 'Osaka',
  startDate: '2026-11-01',
  endDate: '2026-11-02',
  days: 2,
}

describe('trips store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  describe('load', () => {
    it('loads trips from the api', async () => {
      vi.mocked(fetchTrips).mockResolvedValue([tokyo])
      const store = useTripsStore()

      await store.load()

      expect(store.trips).toEqual([tokyo])
      expect(store.loading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('keeps the error message when loading fails', async () => {
      vi.mocked(fetchTrips).mockRejectedValue(new Error('Network error'))
      const store = useTripsStore()

      await store.load()

      expect(store.trips).toEqual([])
      expect(store.loading).toBe(false)
      expect(store.error).toBe('Network error')
    })
  })

  describe('create', () => {
    it('adds the created trip sorted by start date', async () => {
      vi.mocked(fetchTrips).mockResolvedValue([tokyo])
      vi.mocked(createTrip).mockResolvedValue(osaka)
      const store = useTripsStore()
      await store.load()

      const created = await store.create({
        name: 'Osaka',
        startDate: '2026-11-01',
        endDate: '2026-11-02',
      })

      expect(created).toBe(true)
      expect(store.trips.map((trip) => trip.name)).toEqual(['Osaka', 'Tokyo'])
    })

    it('keeps the error message and does not add the trip when creating fails', async () => {
      vi.mocked(createTrip).mockRejectedValue(new Error('A trip cannot be longer than 30 days.'))
      const store = useTripsStore()

      const created = await store.create({
        name: 'Long',
        startDate: '2026-12-01',
        endDate: '2027-01-31',
      })

      expect(created).toBe(false)
      expect(store.trips).toEqual([])
      expect(store.error).toBe('A trip cannot be longer than 30 days.')
    })
  })

  describe('remove', () => {
    it('removes the trip after deleting it', async () => {
      vi.mocked(fetchTrips).mockResolvedValue([osaka, tokyo])
      vi.mocked(deleteTrip).mockResolvedValue()
      const store = useTripsStore()
      await store.load()

      await store.remove(tokyo.id)

      expect(deleteTrip).toHaveBeenCalledWith(tokyo.id)
      expect(store.trips).toEqual([osaka])
    })

    it('marks the trip as deleting until the request finishes', async () => {
      vi.mocked(fetchTrips).mockResolvedValue([tokyo])
      let finishDelete: () => void = () => {}
      vi.mocked(deleteTrip).mockReturnValue(
        new Promise<void>((resolve) => {
          finishDelete = resolve
        }),
      )
      const store = useTripsStore()
      await store.load()

      const removing = store.remove(tokyo.id)

      expect(store.deletingId).toBe(tokyo.id)
      finishDelete()
      await removing
      expect(store.deletingId).toBeNull()
    })

    it('keeps the trip and the error message when deleting fails', async () => {
      vi.mocked(fetchTrips).mockResolvedValue([tokyo])
      vi.mocked(deleteTrip).mockRejectedValue(new Error('Request failed with status 404'))
      const store = useTripsStore()
      await store.load()

      await store.remove(tokyo.id)

      expect(store.trips).toEqual([tokyo])
      expect(store.error).toBe('Request failed with status 404')
      expect(store.deletingId).toBeNull()
    })
  })

  describe('clearError', () => {
    it('removes the error message', async () => {
      vi.mocked(fetchTrips).mockRejectedValue(new Error('Network error'))
      const store = useTripsStore()
      await store.load()

      store.clearError()

      expect(store.error).toBeNull()
    })
  })
})

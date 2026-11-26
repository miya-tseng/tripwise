import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import {
  addActivity,
  deleteActivity,
  fetchSchedule,
  fetchSuggestions,
  moveActivity,
  updateActivity,
} from '@/api/activities'
import { fetchExchangeRates } from '@/api/exchangeRates'
import { fetchTrip, updateTrip } from '@/api/trips'
import type { Activity, SaveActivityInput } from '@/types/activity'
import type { Trip } from '@/types/trip'
import { useScheduleStore } from '../schedule'

vi.mock('@/api/activities')
vi.mock('@/api/trips')
vi.mock('@/api/exchangeRates')

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-02',
  days: 2,
}

const shibuya: Activity = {
  id: 'shibuya-id',
  name: 'Shibuya',
  date: '2026-12-01',
  startTime: '15:00:00',
  endTime: '16:00:00',
  cost: 0,
  currency: 'TWD',
  costInTwd: 0,
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

const sensoji: Activity = {
  id: 'senso-ji-id',
  name: 'Senso-ji',
  date: '2026-12-01',
  startTime: '10:00:00',
  endTime: '11:00:00',
  cost: 0,
  currency: 'TWD',
  costInTwd: 0,
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

const input: SaveActivityInput = {
  name: 'Senso-ji',
  date: '2026-12-01',
  startTime: '10:00',
  endTime: '11:00',
  cost: 0,
  currency: 'TWD',
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

async function loadedStore() {
  vi.mocked(fetchTrip).mockResolvedValue(tokyo)
  vi.mocked(fetchSchedule).mockResolvedValue({
    tripId: tokyo.id,
    ratesAvailable: true,
    ratesUpdatedAt: '2026-01-01T00:00:00Z',
    days: [
      { date: '2026-12-01', activities: [shibuya] },
      { date: '2026-12-02', activities: [] },
    ],
  })
  const store = useScheduleStore()
  await store.load(tokyo.id)
  return store
}

function namesPerDay(store: ReturnType<typeof useScheduleStore>): string[][] {
  return store.days.map((day) => day.activities.map((activity) => activity.name))
}

describe('schedule store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  describe('exchange rates', () => {
    it('loads the exchange rates with the schedule', async () => {
      const rates = {
        baseCurrency: 'TWD',
        available: true,
        updatedAt: '2026-01-01T00:00:00Z',
        currencies: ['JPY', 'TWD'],
        unitsPerBaseCurrency: { TWD: 1, JPY: 4.8 },
      }
      vi.mocked(fetchExchangeRates).mockResolvedValue(rates)

      const store = await loadedStore()

      expect(store.exchangeRates).toEqual(rates)
      expect(store.ratesAvailable).toBe(true)
      expect(store.ratesUpdatedAt).toBe('2026-01-01T00:00:00Z')
    })

    it('still loads the schedule when the exchange rates fail', async () => {
      vi.mocked(fetchExchangeRates).mockRejectedValue(new Error('Cannot connect to the server.'))

      const store = await loadedStore()

      expect(store.exchangeRates).toBeNull()
      expect(store.loadError).toBeNull()
      expect(store.trip).toEqual(tokyo)
    })

    it('keeps whether the schedule rates are available', async () => {
      vi.mocked(fetchTrip).mockResolvedValue(tokyo)
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: tokyo.id,
        ratesAvailable: false,
        ratesUpdatedAt: null,
        days: [],
      })
      const store = useScheduleStore()

      await store.load(tokyo.id)

      expect(store.ratesAvailable).toBe(false)
      expect(store.ratesUpdatedAt).toBeNull()
    })
  })

  describe('load', () => {
    it('loads the trip and its schedule', async () => {
      const store = await loadedStore()

      expect(fetchTrip).toHaveBeenCalledWith('tokyo-id')
      expect(fetchSchedule).toHaveBeenCalledWith('tokyo-id')
      expect(store.trip).toEqual(tokyo)
      expect(namesPerDay(store)).toEqual([['Shibuya'], []])
      expect(store.loading).toBe(false)
      expect(store.loadError).toBeNull()
    })

    it('keeps the load error when the trip cannot be loaded', async () => {
      vi.mocked(fetchTrip).mockRejectedValue(new Error('Request failed with status 404'))
      vi.mocked(fetchSchedule).mockRejectedValue(new Error('Request failed with status 404'))
      const store = useScheduleStore()

      await store.load('missing')

      expect(store.loadError).toBe('Request failed with status 404')
      expect(store.trip).toBeNull()
      expect(store.loading).toBe(false)
    })
  })

  describe('add', () => {
    it('adds the activity in start time order', async () => {
      const store = await loadedStore()
      vi.mocked(addActivity).mockResolvedValue(sensoji)

      const added = await store.add(input)

      expect(added).toBe(true)
      expect(addActivity).toHaveBeenCalledWith('tokyo-id', input)
      expect(namesPerDay(store)).toEqual([['Senso-ji', 'Shibuya'], []])
    })

    it('keeps the overlap message and the schedule when the activity overlaps', async () => {
      const store = await loadedStore()
      vi.mocked(addActivity).mockRejectedValue(new Error('The activity overlaps with "Shibuya"'))

      const added = await store.add({ ...input, startTime: '15:30', endTime: '16:30' })

      expect(added).toBe(false)
      expect(store.error).toBe('The activity overlaps with "Shibuya"')
      expect(namesPerDay(store)).toEqual([['Shibuya'], []])
    })
  })

  describe('update', () => {
    it('moves the updated activity to its new day', async () => {
      const store = await loadedStore()
      vi.mocked(updateActivity).mockResolvedValue({ ...shibuya, date: '2026-12-02' })

      const updated = await store.update(shibuya.id, { ...input, date: '2026-12-02' })

      expect(updated).toBe(true)
      expect(updateActivity).toHaveBeenCalledWith('tokyo-id', 'shibuya-id', {
        ...input,
        date: '2026-12-02',
      })
      expect(namesPerDay(store)).toEqual([[], ['Shibuya']])
    })

    it('keeps the error when the update fails', async () => {
      const store = await loadedStore()
      vi.mocked(updateActivity).mockRejectedValue(new Error('Overlap'))

      const updated = await store.update(shibuya.id, input)

      expect(updated).toBe(false)
      expect(store.error).toBe('Overlap')
      expect(namesPerDay(store)).toEqual([['Shibuya'], []])
    })
  })

  describe('editTrip', () => {
    const input = { name: 'Osaka', startDate: '2026-12-01', endDate: '2026-12-03' }

    it('updates the trip and reloads the schedule', async () => {
      const store = await loadedStore()
      const updated = { ...tokyo, name: 'Osaka', endDate: '2026-12-03', days: 3 }
      vi.mocked(updateTrip).mockResolvedValue(updated)
      vi.mocked(fetchSchedule).mockResolvedValue({
        tripId: tokyo.id,
        ratesAvailable: true,
        ratesUpdatedAt: '2026-01-01T00:00:00Z',
        days: [
          { date: '2026-12-01', activities: [shibuya] },
          { date: '2026-12-02', activities: [] },
          { date: '2026-12-03', activities: [] },
        ],
      })

      const saved = await store.editTrip(input)

      expect(saved).toBe(true)
      expect(updateTrip).toHaveBeenCalledWith('tokyo-id', input)
      expect(store.trip).toEqual(updated)
      expect(store.days.map((day) => day.date)).toEqual(['2026-12-01', '2026-12-02', '2026-12-03'])
      expect(store.editTripError).toBeNull()
    })

    it('keeps the trip and the error when the update is rejected', async () => {
      const store = await loadedStore()
      vi.mocked(updateTrip).mockRejectedValue(new Error('1 activity falls outside the new dates.'))

      const saved = await store.editTrip(input)

      expect(saved).toBe(false)
      expect(store.trip).toEqual(tokyo)
      expect(store.editTripError).toBe('1 activity falls outside the new dates.')
    })

    it('clears the edit error', async () => {
      const store = await loadedStore()
      vi.mocked(updateTrip).mockRejectedValue(new Error('Conflict'))
      await store.editTrip(input)

      store.clearEditTripError()

      expect(store.editTripError).toBeNull()
    })
  })

  describe('remove', () => {
    it('removes the activity from the schedule', async () => {
      const store = await loadedStore()
      vi.mocked(deleteActivity).mockResolvedValue()

      await store.remove(shibuya.id)

      expect(deleteActivity).toHaveBeenCalledWith('tokyo-id', 'shibuya-id')
      expect(namesPerDay(store)).toEqual([[], []])
      expect(store.deletingId).toBeNull()
    })

    it('keeps the activity when the deletion fails', async () => {
      const store = await loadedStore()
      vi.mocked(deleteActivity).mockRejectedValue(new Error('Cannot connect to the server.'))

      await store.remove(shibuya.id)

      expect(store.error).toBe('Cannot connect to the server.')
      expect(namesPerDay(store)).toEqual([['Shibuya'], []])
    })
  })

  describe('suggestions', () => {
    const suggestion = {
      place: {
        placeId: 'skytree-id',
        name: 'Tokyo Skytree',
        address: 'Oshiage, Tokyo',
        latitude: 35.71,
        longitude: 139.81,
        googleMapsUri: 'https://maps.google.com/?cid=1',
      },
      reason: 'Close by',
      suggestedMinutes: 90,
    }

    it('loads suggestions for an activity', async () => {
      const store = await loadedStore()
      vi.mocked(fetchSuggestions).mockResolvedValue([suggestion])

      await store.loadSuggestions(shibuya)

      expect(fetchSuggestions).toHaveBeenCalledWith('tokyo-id', 'shibuya-id')
      expect(store.suggestionsFor).toEqual(shibuya)
      expect(store.suggestions).toEqual([suggestion])
      expect(store.suggestionsLoading).toBe(false)
      expect(store.suggestionsError).toBeNull()
    })

    it('keeps the error when suggestions fail', async () => {
      const store = await loadedStore()
      vi.mocked(fetchSuggestions).mockRejectedValue(
        new Error('Suggestions are unavailable right now.'),
      )

      await store.loadSuggestions(shibuya)

      expect(store.suggestionsError).toBe('Suggestions are unavailable right now.')
      expect(store.suggestions).toEqual([])
      expect(store.suggestionsLoading).toBe(false)
    })

    it('ignores suggestions that arrive after the panel was closed', async () => {
      const store = await loadedStore()
      let resolve: (value: (typeof suggestion)[]) => void = () => {}
      vi.mocked(fetchSuggestions).mockReturnValue(new Promise((done) => (resolve = done)))

      const loading = store.loadSuggestions(shibuya)
      store.closeSuggestions()
      resolve([suggestion])
      await loading

      expect(store.suggestionsFor).toBeNull()
      expect(store.suggestions).toEqual([])
    })

    it('drops a suggestion once its place is added', async () => {
      const store = await loadedStore()
      vi.mocked(fetchSuggestions).mockResolvedValue([suggestion])
      await store.loadSuggestions(shibuya)

      store.dropSuggestion('skytree-id')

      expect(store.suggestions).toEqual([])
    })
  })

  describe('move', () => {
    it('moves the activity to its new day', async () => {
      const store = await loadedStore()
      vi.mocked(moveActivity).mockResolvedValue({ ...shibuya, date: '2026-12-02' })

      const moved = await store.move(shibuya.id, { date: '2026-12-02', startTime: '15:00' })

      expect(moved).toBe(true)
      expect(moveActivity).toHaveBeenCalledWith('tokyo-id', 'shibuya-id', {
        date: '2026-12-02',
        startTime: '15:00',
      })
      expect(namesPerDay(store)).toEqual([[], ['Shibuya']])
    })

    it('keeps the schedule and the error when the move is rejected', async () => {
      const store = await loadedStore()
      vi.mocked(moveActivity).mockRejectedValue(new Error('The activity overlaps with "Senso-ji"'))

      const moved = await store.move(shibuya.id, { date: '2026-12-02', startTime: '15:00' })

      expect(moved).toBe(false)
      expect(store.error).toBe('The activity overlaps with "Senso-ji"')
      expect(namesPerDay(store)).toEqual([['Shibuya'], []])
    })
  })
})

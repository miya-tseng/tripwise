import { ref } from 'vue'
import { defineStore } from 'pinia'
import { createTrip, deleteTrip, fetchTrips } from '@/api/trips'
import type { CreateTripInput, Trip } from '@/types/trip'

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong.'
}

function byStartDate(a: Trip, b: Trip): number {
  return a.startDate.localeCompare(b.startDate)
}

export const useTripsStore = defineStore('trips', () => {
  const trips = ref<Trip[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const deletingId = ref<string | null>(null)

  async function load(): Promise<void> {
    loading.value = true
    error.value = null

    try {
      trips.value = await fetchTrips()
    } catch (e) {
      error.value = toMessage(e)
    } finally {
      loading.value = false
    }
  }

  async function create(input: CreateTripInput): Promise<boolean> {
    error.value = null

    try {
      const trip = await createTrip(input)
      trips.value = [...trips.value, trip].sort(byStartDate)
      return true
    } catch (e) {
      error.value = toMessage(e)
      return false
    }
  }

  async function remove(id: string): Promise<void> {
    error.value = null
    deletingId.value = id

    try {
      await deleteTrip(id)
      trips.value = trips.value.filter((trip) => trip.id !== id)
    } catch (e) {
      error.value = toMessage(e)
    } finally {
      deletingId.value = null
    }
  }

  function clearError(): void {
    error.value = null
  }

  return { trips, loading, error, deletingId, load, create, remove, clearError }
})

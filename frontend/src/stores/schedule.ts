import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  addActivity,
  deleteActivity,
  fetchSchedule,
  fetchSuggestions,
  moveActivity,
  updateActivity,
  type MoveActivityInput,
} from '@/api/activities'
import { fetchExchangeRates } from '@/api/exchangeRates'
import { fetchTrip, updateTrip } from '@/api/trips'
import type { Activity, SaveActivityInput, Schedule, ScheduleDay } from '@/types/activity'
import type { ExchangeRates } from '@/types/currency'
import type { Suggestion } from '@/types/suggestion'
import type { Trip, UpdateTripInput } from '@/types/trip'
import { withActivity, withoutActivity } from '@/utils/schedule'

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong.'
}

export const useScheduleStore = defineStore('schedule', () => {
  const trip = ref<Trip | null>(null)
  const days = ref<ScheduleDay[]>([])
  const loading = ref(false)
  const loadError = ref<string | null>(null)
  const error = ref<string | null>(null)
  const deletingId = ref<string | null>(null)
  const editTripError = ref<string | null>(null)
  const ratesAvailable = ref(true)
  const ratesUpdatedAt = ref<string | null>(null)
  const exchangeRates = ref<ExchangeRates | null>(null)
  const suggestionsFor = ref<Activity | null>(null)
  const suggestions = ref<Suggestion[]>([])
  const suggestionsLoading = ref(false)
  const suggestionsError = ref<string | null>(null)
  let suggestionsRequest = 0

  function applySchedule(schedule: Schedule): void {
    days.value = schedule.days
    ratesAvailable.value = schedule.ratesAvailable
    ratesUpdatedAt.value = schedule.ratesUpdatedAt
  }

  async function loadExchangeRates(): Promise<void> {
    try {
      exchangeRates.value = await fetchExchangeRates()
    } catch {
      exchangeRates.value = null
    }
  }

  async function load(tripId: string): Promise<void> {
    loading.value = true
    loadError.value = null
    error.value = null
    trip.value = null
    days.value = []

    try {
      const [loadedTrip, schedule] = await Promise.all([
        fetchTrip(tripId),
        fetchSchedule(tripId),
        loadExchangeRates(),
      ])
      trip.value = loadedTrip
      applySchedule(schedule)
    } catch (e) {
      loadError.value = toMessage(e)
    } finally {
      loading.value = false
    }
  }

  async function add(input: SaveActivityInput): Promise<boolean> {
    if (!trip.value) {
      return false
    }

    error.value = null

    try {
      const activity = await addActivity(trip.value.id, input)
      days.value = withActivity(days.value, activity)
      return true
    } catch (e) {
      error.value = toMessage(e)
      return false
    }
  }

  async function update(activityId: string, input: SaveActivityInput): Promise<boolean> {
    if (!trip.value) {
      return false
    }

    error.value = null

    try {
      const activity = await updateActivity(trip.value.id, activityId, input)
      days.value = withActivity(days.value, activity)
      return true
    } catch (e) {
      error.value = toMessage(e)
      return false
    }
  }

  async function move(activityId: string, input: MoveActivityInput): Promise<boolean> {
    if (!trip.value) {
      return false
    }

    error.value = null

    try {
      const activity = await moveActivity(trip.value.id, activityId, input)
      days.value = withActivity(days.value, activity)
      return true
    } catch (e) {
      error.value = toMessage(e)
      return false
    }
  }

  async function remove(activityId: string): Promise<void> {
    if (!trip.value) {
      return
    }

    error.value = null
    deletingId.value = activityId

    try {
      await deleteActivity(trip.value.id, activityId)
      days.value = withoutActivity(days.value, activityId)
    } catch (e) {
      error.value = toMessage(e)
    } finally {
      deletingId.value = null
    }
  }

  async function editTrip(input: UpdateTripInput): Promise<boolean> {
    if (!trip.value) {
      return false
    }

    editTripError.value = null

    try {
      const updated = await updateTrip(trip.value.id, input)
      const schedule = await fetchSchedule(updated.id)
      trip.value = updated
      applySchedule(schedule)
      return true
    } catch (e) {
      editTripError.value = toMessage(e)
      return false
    }
  }

  async function loadSuggestions(activity: Activity): Promise<void> {
    if (!trip.value) {
      return
    }

    const requestId = ++suggestionsRequest
    suggestionsFor.value = activity
    suggestions.value = []
    suggestionsError.value = null
    suggestionsLoading.value = true

    try {
      const result = await fetchSuggestions(trip.value.id, activity.id)
      if (requestId === suggestionsRequest) {
        suggestions.value = result
      }
    } catch (e) {
      if (requestId === suggestionsRequest) {
        suggestionsError.value = toMessage(e)
      }
    } finally {
      if (requestId === suggestionsRequest) {
        suggestionsLoading.value = false
      }
    }
  }

  function closeSuggestions(): void {
    suggestionsRequest++
    suggestionsFor.value = null
    suggestions.value = []
    suggestionsError.value = null
    suggestionsLoading.value = false
  }

  function dropSuggestion(placeId: string): void {
    suggestions.value = suggestions.value.filter(
      (suggestion) => suggestion.place.placeId !== placeId,
    )
  }

  function clearEditTripError(): void {
    editTripError.value = null
  }

  function clearError(): void {
    error.value = null
  }

  return {
    trip,
    days,
    loading,
    loadError,
    error,
    deletingId,
    editTripError,
    ratesAvailable,
    ratesUpdatedAt,
    exchangeRates,
    suggestionsFor,
    suggestions,
    suggestionsLoading,
    suggestionsError,
    load,
    add,
    update,
    move,
    remove,
    clearError,
    editTrip,
    clearEditTripError,
    loadSuggestions,
    closeSuggestions,
    dropSuggestion,
  }
})

import { ref } from 'vue'
import { defineStore } from 'pinia'
import { addActivity, deleteActivity, fetchSchedule, updateActivity } from '@/api/activities'
import { fetchTrip } from '@/api/trips'
import type { SaveActivityInput, ScheduleDay } from '@/types/activity'
import type { Trip } from '@/types/trip'
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

  async function load(tripId: string): Promise<void> {
    loading.value = true
    loadError.value = null
    error.value = null
    trip.value = null
    days.value = []

    try {
      const [loadedTrip, schedule] = await Promise.all([fetchTrip(tripId), fetchSchedule(tripId)])
      trip.value = loadedTrip
      days.value = schedule.days
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
    load,
    add,
    update,
    remove,
    clearError,
  }
})

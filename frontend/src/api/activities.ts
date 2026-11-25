import type { Activity, SaveActivityInput, Schedule } from '@/types/activity'
import { jsonBody, request } from './http'

function tripUrl(tripId: string): string {
  return `/api/trips/${tripId}`
}

export async function fetchSchedule(tripId: string): Promise<Schedule> {
  const response = await request(`${tripUrl(tripId)}/schedule`)
  return response.json()
}

export async function addActivity(tripId: string, input: SaveActivityInput): Promise<Activity> {
  const response = await request(`${tripUrl(tripId)}/activities`, jsonBody('POST', input))
  return response.json()
}

export async function updateActivity(
  tripId: string,
  activityId: string,
  input: SaveActivityInput,
): Promise<Activity> {
  const response = await request(
    `${tripUrl(tripId)}/activities/${activityId}`,
    jsonBody('PUT', input),
  )
  return response.json()
}

export async function deleteActivity(tripId: string, activityId: string): Promise<void> {
  await request(`${tripUrl(tripId)}/activities/${activityId}`, { method: 'DELETE' })
}

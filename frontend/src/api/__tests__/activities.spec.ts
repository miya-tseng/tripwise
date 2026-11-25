import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Activity, Schedule } from '@/types/activity'
import { addActivity, deleteActivity, fetchSchedule, updateActivity } from '../activities'

const tripId = 'trip-id'

const sensoji: Activity = {
  id: 'senso-ji-id',
  name: 'Senso-ji',
  date: '2026-12-01',
  startTime: '10:00:00',
  endTime: '11:00:00',
  cost: 0,
}

const input = {
  name: 'Senso-ji',
  date: '2026-12-01',
  startTime: '10:00',
  endTime: '11:00',
  cost: 0,
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('activities api', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('gets the schedule of a trip', async () => {
    const schedule: Schedule = { tripId, days: [{ date: '2026-12-01', activities: [sensoji] }] }
    fetchMock.mockResolvedValue(jsonResponse(schedule))

    const result = await fetchSchedule(tripId)

    expect(fetchMock).toHaveBeenCalledWith('/api/trips/trip-id/schedule')
    expect(result).toEqual(schedule)
  })

  it('posts a new activity', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sensoji, 201))

    const result = await addActivity(tripId, input)

    expect(fetchMock).toHaveBeenCalledWith('/api/trips/trip-id/activities', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    })
    expect(result).toEqual(sensoji)
  })

  it('throws the overlap detail when the activity overlaps', async () => {
    const detail = 'The activity overlaps with "Tsukiji" (09:30-10:30) on 2026-12-01.'
    fetchMock.mockResolvedValue(jsonResponse({ title: 'Activity overlap', detail }, 409))

    await expect(addActivity(tripId, input)).rejects.toMatchObject({
      status: 409,
      message: detail,
    })
  })

  it('puts an updated activity', async () => {
    fetchMock.mockResolvedValue(jsonResponse(sensoji))

    const result = await updateActivity(tripId, sensoji.id, input)

    expect(fetchMock).toHaveBeenCalledWith('/api/trips/trip-id/activities/senso-ji-id', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    })
    expect(result).toEqual(sensoji)
  })

  it('deletes an activity', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await deleteActivity(tripId, sensoji.id)

    expect(fetchMock).toHaveBeenCalledWith('/api/trips/trip-id/activities/senso-ji-id', {
      method: 'DELETE',
    })
  })
})

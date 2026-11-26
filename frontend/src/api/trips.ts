import type { CreateTripInput, Trip, UpdateTripInput } from '@/types/trip'
import { jsonBody, request } from './http'

export { ApiError } from './http'

const baseUrl = '/api/trips'

export async function fetchTrips(): Promise<Trip[]> {
  const response = await request(baseUrl)
  return response.json()
}

export async function fetchTrip(id: string): Promise<Trip> {
  const response = await request(`${baseUrl}/${id}`)
  return response.json()
}

export async function createTrip(input: CreateTripInput): Promise<Trip> {
  const response = await request(baseUrl, jsonBody('POST', input))
  return response.json()
}

export async function updateTrip(id: string, input: UpdateTripInput): Promise<Trip> {
  const response = await request(`${baseUrl}/${id}`, jsonBody('PUT', input))
  return response.json()
}

export async function deleteTrip(id: string): Promise<void> {
  await request(`${baseUrl}/${id}`, { method: 'DELETE' })
}

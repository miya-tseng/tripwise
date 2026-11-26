import type { SaveStayInput, Stay } from '@/types/stay'
import { jsonBody, request } from './http'

function staysUrl(tripId: string): string {
  return `/api/trips/${tripId}/stays`
}

export async function addStay(tripId: string, input: SaveStayInput): Promise<Stay> {
  const response = await request(staysUrl(tripId), jsonBody('POST', input))
  return response.json()
}

export async function updateStay(
  tripId: string,
  stayId: string,
  input: SaveStayInput,
): Promise<Stay> {
  const response = await request(`${staysUrl(tripId)}/${stayId}`, jsonBody('PUT', input))
  return response.json()
}

export async function deleteStay(tripId: string, stayId: string): Promise<void> {
  await request(`${staysUrl(tripId)}/${stayId}`, { method: 'DELETE' })
}

import type {
  BookingItem,
  Checklist,
  ChecklistSection,
  SaveChecklistItemInput,
} from '@/types/checklist'
import { jsonBody, request } from './http'

function checklistUrl(tripId: string): string {
  return `/api/trips/${tripId}/checklist`
}

async function toChecklist(response: Response): Promise<Checklist> {
  return response.json()
}

export async function fetchChecklist(tripId: string): Promise<Checklist> {
  return toChecklist(await request(checklistUrl(tripId)))
}

export async function addChecklistItem(
  tripId: string,
  input: SaveChecklistItemInput,
): Promise<Checklist> {
  return toChecklist(await request(checklistUrl(tripId), jsonBody('POST', input)))
}

export async function updateChecklistItem(
  tripId: string,
  itemId: string,
  input: SaveChecklistItemInput,
): Promise<Checklist> {
  return toChecklist(await request(`${checklistUrl(tripId)}/${itemId}`, jsonBody('PUT', input)))
}

export async function setChecklistItemDone(
  tripId: string,
  itemId: string,
  done: boolean,
): Promise<Checklist> {
  return toChecklist(
    await request(`${checklistUrl(tripId)}/${itemId}/done`, jsonBody('PUT', { done })),
  )
}

export async function deleteChecklistItem(tripId: string, itemId: string): Promise<Checklist> {
  return toChecklist(await request(`${checklistUrl(tripId)}/${itemId}`, { method: 'DELETE' }))
}

export async function reorderChecklist(
  tripId: string,
  section: ChecklistSection,
  itemIds: string[],
): Promise<Checklist> {
  return toChecklist(
    await request(`${checklistUrl(tripId)}/order`, jsonBody('PUT', { section, itemIds })),
  )
}

export async function setBookingDone(
  tripId: string,
  booking: Pick<BookingItem, 'kind' | 'id'>,
  done: boolean,
): Promise<Checklist> {
  const kind = booking.kind === 'Stay' ? 'stays' : 'activities'
  return toChecklist(
    await request(
      `${checklistUrl(tripId)}/bookings/${kind}/${booking.id}`,
      jsonBody('PUT', { done }),
    ),
  )
}

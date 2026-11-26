import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  addChecklistItem,
  deleteChecklistItem,
  fetchChecklist,
  reorderChecklist,
  setBookingDone,
  setChecklistItemDone,
  updateChecklistItem,
} from '@/api/checklist'
import type {
  BookingItem,
  Checklist,
  ChecklistSection,
  SaveChecklistItemInput,
} from '@/types/checklist'

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong.'
}

export const useChecklistStore = defineStore('checklist', () => {
  const checklist = ref<Checklist | null>(null)
  const tripId = ref<string | null>(null)
  const loading = ref(false)
  const loadError = ref<string | null>(null)
  const error = ref<string | null>(null)

  async function load(id: string): Promise<void> {
    tripId.value = id
    loading.value = true
    loadError.value = null
    error.value = null

    try {
      checklist.value = await fetchChecklist(id)
    } catch (e) {
      checklist.value = null
      loadError.value = toMessage(e)
    } finally {
      loading.value = false
    }
  }

  async function run(
    change: (id: string) => Promise<Checklist>,
    optimistic?: (current: Checklist) => Checklist,
  ): Promise<boolean> {
    if (!tripId.value) {
      return false
    }

    const previous = checklist.value
    error.value = null

    if (previous && optimistic) {
      checklist.value = optimistic(previous)
    }

    try {
      checklist.value = await change(tripId.value)
      return true
    } catch (e) {
      checklist.value = previous
      error.value = toMessage(e)
      return false
    }
  }

  function add(input: SaveChecklistItemInput): Promise<boolean> {
    return run((id) => addChecklistItem(id, input))
  }

  function update(itemId: string, input: SaveChecklistItemInput): Promise<boolean> {
    return run((id) => updateChecklistItem(id, itemId, input))
  }

  function toggle(itemId: string, done: boolean): Promise<boolean> {
    return run(
      (id) => setChecklistItemDone(id, itemId, done),
      (current) => ({
        ...current,
        items: current.items.map((item) => (item.id === itemId ? { ...item, done } : item)),
      }),
    )
  }

  function remove(itemId: string): Promise<boolean> {
    return run((id) => deleteChecklistItem(id, itemId))
  }

  function reorder(section: ChecklistSection, itemIds: string[]): Promise<boolean> {
    const order = new Map(itemIds.map((itemId, index) => [itemId, index]))

    return run(
      (id) => reorderChecklist(id, section, itemIds),
      (current) => ({
        ...current,
        items: current.items.map((item) => ({
          ...item,
          position: order.get(item.id) ?? item.position,
        })),
      }),
    )
  }

  function toggleBooking(booking: BookingItem, done: boolean): Promise<boolean> {
    return run((id) => setBookingDone(id, booking, done))
  }

  function clearError(): void {
    error.value = null
  }

  return {
    checklist,
    loading,
    loadError,
    error,
    load,
    add,
    update,
    toggle,
    remove,
    reorder,
    toggleBooking,
    clearError,
  }
})

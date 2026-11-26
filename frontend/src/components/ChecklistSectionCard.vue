<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import SelectMenu from '@/components/SelectMenu.vue'
import type {
  BookingItem,
  ChecklistItem,
  ChecklistSection,
  SaveChecklistItemInput,
} from '@/types/checklist'
import { MAX_CHECKLIST_TEXT_LENGTH } from '@/types/checklist'
import { bookingStatusMeta } from '@/utils/activityDetails'
import { SECTIONS, dueLabel, isOverdue, moveId, type SectionMeta } from '@/utils/checklist'
import { formatDayLabel } from '@/utils/date'

const props = withDefaults(
  defineProps<{
    section: SectionMeta
    items: ChecklistItem[]
    today: string
    bookings?: BookingItem[]
    suggestions?: string[]
  }>(),
  {
    bookings: () => [],
    suggestions: () => [],
  },
)

const emit = defineEmits<{
  add: [text: string]
  toggle: [itemId: string, done: boolean]
  update: [itemId: string, input: SaveChecklistItemInput]
  remove: [itemId: string]
  reorder: [itemIds: string[]]
  toggleBooking: [booking: BookingItem, done: boolean]
}>()

const newText = ref('')
const editingId = ref<string | null>(null)
const editText = ref('')
const editSection = ref<ChecklistSection>('Other')
const editDueDate = ref('')
const draggingId = ref<string | null>(null)
const dropIndex = ref<number | null>(null)
const editInput = ref<HTMLInputElement[] | null>(null)
const addedSuggestions = ref<string[]>([])

const sectionOptions = SECTIONS.map((section) => ({ value: section.value, label: section.label }))
const done = computed(
  () =>
    props.items.filter((item) => item.done).length +
    props.bookings.filter((booking) => booking.done).length,
)
const total = computed(() => props.items.length + props.bookings.length)
const ids = computed(() => props.items.map((item) => item.id))
const visibleSuggestions = computed(() =>
  props.suggestions.filter((suggestion) => !addedSuggestions.value.includes(suggestion)),
)

function addSuggestion(suggestion: string) {
  addedSuggestions.value = [...addedSuggestions.value, suggestion]
  emit('add', suggestion)
}

function submitNew() {
  const text = newText.value.trim()

  if (text === '') {
    return
  }

  emit('add', text)
  newText.value = ''
}

async function startEditing(item: ChecklistItem) {
  editingId.value = item.id
  editText.value = item.text
  editSection.value = item.section
  editDueDate.value = item.dueDate ?? ''
  await nextTick()
  editInput.value?.[0]?.focus()
}

function saveEdit(item: ChecklistItem) {
  const text = editText.value.trim()

  if (text === '') {
    return
  }

  emit('update', item.id, {
    text,
    section: editSection.value,
    dueDate: editDueDate.value === '' ? null : editDueDate.value,
  })
  editingId.value = null
}

function removeItem(item: ChecklistItem) {
  editingId.value = null
  emit('remove', item.id)
}

function move(item: ChecklistItem, step: number) {
  const index = ids.value.indexOf(item.id)
  const next = moveId(ids.value, item.id, index + step)

  if (next.join() !== ids.value.join()) {
    emit('reorder', next)
  }
}

function onDragStart(event: DragEvent, item: ChecklistItem) {
  draggingId.value = item.id
  event.dataTransfer?.setData('text/plain', item.id)

  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

function onDragOver(event: DragEvent, index: number) {
  if (!draggingId.value) {
    return
  }

  event.preventDefault()
  const target = event.currentTarget as HTMLElement
  const box = target.getBoundingClientRect()
  dropIndex.value = event.clientY > box.top + box.height / 2 ? index + 1 : index
}

function onDrop(event: DragEvent) {
  event.preventDefault()
  const id = draggingId.value
  const target = dropIndex.value

  if (id && target !== null) {
    const from = ids.value.indexOf(id)
    const next = moveId(ids.value, id, target > from ? target - 1 : target)

    if (next.join() !== ids.value.join()) {
      emit('reorder', next)
    }
  }

  onDragEnd()
}

function onDragEnd() {
  draggingId.value = null
  dropIndex.value = null
}
</script>

<template>
  <section
    class="card checklist-section"
    :aria-labelledby="`section-${section.value}`"
    :data-testid="`section-${section.value}`"
  >
    <header class="section-head">
      <h2 :id="`section-${section.value}`" class="section-name">
        <span aria-hidden="true">{{ section.icon }}</span> {{ section.label }}
      </h2>
      <span class="section-count" data-testid="section-count">{{ done }} / {{ total }}</span>
    </header>

    <ul v-if="bookings.length > 0" class="items bookings" aria-label="From your plan">
      <li
        v-for="booking in bookings"
        :key="booking.id"
        class="item booking"
        :class="{ done: booking.done }"
        data-testid="booking-item"
      >
        <label class="check">
          <input
            type="checkbox"
            :checked="booking.done"
            :aria-label="`${booking.name} booked`"
            data-testid="booking-checkbox"
            @change="emit('toggleBooking', booking, ($event.target as HTMLInputElement).checked)"
          />
          <span class="box" aria-hidden="true" />
        </label>
        <span class="item-body">
          <span class="item-text" data-testid="booking-name">
            {{ booking.kind === 'Stay' ? '🛏' : '🔗' }} {{ booking.name }}
          </span>
          <span class="item-meta">
            {{ booking.kind === 'Stay' ? 'Check-in' : 'From your plan' }} ·
            {{ formatDayLabel(booking.date) }}
          </span>
        </span>
        <span
          class="booking-badge"
          :class="`tone-${bookingStatusMeta(booking.bookingStatus).tone}`"
          data-testid="booking-item-status"
        >
          {{ bookingStatusMeta(booking.bookingStatus).label }}
        </span>
      </li>
    </ul>

    <ul class="items" @dragleave.self="dropIndex = null">
      <li
        v-for="(item, index) in items"
        :key="item.id"
        class="item"
        :class="{
          done: item.done,
          overdue: isOverdue(item, today),
          dragging: draggingId === item.id,
          'drop-before': dropIndex === index && draggingId !== item.id,
          'drop-after': dropIndex === index + 1 && index === items.length - 1,
        }"
        :draggable="editingId !== item.id"
        data-testid="checklist-item"
        @dragstart="onDragStart($event, item)"
        @dragover="onDragOver($event, index)"
        @drop="onDrop"
        @dragend="onDragEnd"
      >
        <template v-if="editingId === item.id">
          <form class="edit-form" data-testid="edit-item-form" @submit.prevent="saveEdit(item)">
            <input
              ref="editInput"
              v-model="editText"
              class="text-input"
              type="text"
              aria-label="Item"
              :maxlength="MAX_CHECKLIST_TEXT_LENGTH"
              data-testid="edit-item-text"
              @keydown.esc="editingId = null"
            />
            <div class="edit-row">
              <SelectMenu
                v-model="editSection"
                :options="sectionOptions"
                name="item-section"
                data-testid="edit-item-section"
              />
              <label class="due-field">
                <span class="visually-hidden">Due date</span>
                <input
                  v-model="editDueDate"
                  class="text-input"
                  type="date"
                  aria-label="Due date"
                  data-testid="edit-item-due"
                />
              </label>
            </div>
            <div class="edit-actions">
              <button
                type="button"
                class="btn btn-ghost btn-small delete"
                data-testid="delete-item"
                @click="removeItem(item)"
              >
                Delete
              </button>
              <button
                type="button"
                class="btn btn-secondary btn-small"
                data-testid="cancel-edit-item"
                @click="editingId = null"
              >
                Cancel
              </button>
              <button type="submit" class="btn btn-primary btn-small" data-testid="save-item">
                Save
              </button>
            </div>
          </form>
        </template>
        <template v-else>
          <label class="check">
            <input
              type="checkbox"
              :checked="item.done"
              :aria-label="item.text"
              data-testid="item-checkbox"
              @change="emit('toggle', item.id, ($event.target as HTMLInputElement).checked)"
            />
            <span class="box" aria-hidden="true" />
          </label>
          <button
            type="button"
            class="item-body item-button"
            data-testid="edit-item"
            @click="startEditing(item)"
          >
            <span class="item-text" data-testid="item-text">{{ item.text }}</span>
            <span
              v-if="item.dueDate"
              class="due"
              :class="{ late: isOverdue(item, today) }"
              data-testid="item-due"
            >
              {{ dueLabel(item.dueDate, today) }}
            </span>
          </button>
          <span
            class="handle"
            role="button"
            tabindex="0"
            :aria-label="`Move ${item.text}. Use arrow keys to move up or down`"
            data-testid="drag-handle"
            @keydown.up.prevent="move(item, -1)"
            @keydown.down.prevent="move(item, 1)"
          >
            ⋮⋮
          </span>
        </template>
      </li>
    </ul>

    <form class="quick-add" @submit.prevent="submitNew">
      <input
        v-model="newText"
        class="text-input"
        type="text"
        :placeholder="`Add an item, ${section.placeholder}`"
        :aria-label="`Add to ${section.label}`"
        :maxlength="MAX_CHECKLIST_TEXT_LENGTH"
        data-testid="quick-add"
      />
      <button
        type="submit"
        class="btn btn-secondary btn-small"
        :disabled="newText.trim() === ''"
        data-testid="quick-add-button"
      >
        Add
      </button>
    </form>

    <div v-if="visibleSuggestions.length > 0" class="suggestions" data-testid="suggestions">
      <span class="suggestions-label">Quick add</span>
      <button
        v-for="suggestion in visibleSuggestions"
        :key="suggestion"
        type="button"
        class="suggestion"
        data-testid="suggestion"
        @click="addSuggestion(suggestion)"
      >
        + {{ suggestion }}
      </button>
    </div>
  </section>
</template>

<style scoped>
.checklist-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 1.2rem 1.25rem;
}

.section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-name {
  font-family: var(--font-display);
  font-size: 1.15rem;
  font-weight: 700;
}

.section-count {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--color-muted);
  font-variant-numeric: tabular-nums;
}

.items {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.items:empty {
  display: none;
}

.item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  min-height: 2.75rem;
  padding: 0.35rem 0.5rem 0.35rem 0.6rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 10px;
  transition: background 0.15s;
}

.item.booking {
  background: var(--color-surface-muted);
  border-style: dashed;
}

.item.overdue {
  background: #fdecea;
  border-color: #f2b8b5;
}

.item.dragging {
  opacity: 0.45;
}

.item.drop-before::before,
.item.drop-after::after {
  content: '';
  position: absolute;
  left: 0.5rem;
  right: 0.5rem;
  height: 2px;
  background: var(--color-primary);
  border-radius: 2px;
}

.item.drop-before::before {
  top: -0.25rem;
}

.item.drop-after::after {
  bottom: -0.25rem;
}

.check {
  position: relative;
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 1.75rem;
  height: 1.75rem;
  cursor: pointer;
}

.check input {
  position: absolute;
  inset: 0;
  margin: 0;
  opacity: 0;
  cursor: pointer;
}

.box {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid var(--color-border-strong);
  border-radius: 6px;
  transition:
    background 0.15s,
    border-color 0.15s;
}

.check input:checked + .box {
  background: var(--color-primary)
    url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'%3E%3Cpath d='m5 10.5 3.2 3L15 6.5' fill='none' stroke='white' stroke-width='2.4' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E")
    center / 0.95rem no-repeat;
  border-color: var(--color-primary);
}

.check input:focus-visible + .box {
  box-shadow: 0 0 0 3px var(--color-primary-ring);
}

.item-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  min-width: 0;
}

.item-button {
  padding: 0.2rem 0;
  font: inherit;
  text-align: left;
  color: inherit;
  background: none;
  border: 0;
  cursor: pointer;
}

.item-text {
  max-width: 100%;
  font-size: 0.92rem;
  overflow-wrap: anywhere;
}

.done .item-text {
  color: var(--color-subtle);
  text-decoration: line-through;
}

.item-meta {
  font-size: 0.75rem;
  color: var(--color-muted);
}

.due {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-muted);
}

.due.late {
  color: #b42318;
}

.booking-badge {
  flex-shrink: 0;
  padding: 0.05rem 0.5rem;
  font-size: 0.7rem;
  font-weight: 700;
  color: var(--color-muted);
  background: var(--color-surface);
  border-radius: 999px;
}

.booking-badge.tone-warning {
  color: #9a4a1c;
  background: #fde8d7;
}

.booking-badge.tone-success {
  color: #1f5c56;
  background: #e1efe9;
}

.handle {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 2rem;
  height: 2rem;
  padding: 0;
  font-size: 0.85rem;
  letter-spacing: -0.15em;
  color: var(--color-subtle);
  background: none;
  border: 0;
  border-radius: 6px;
  cursor: grab;
}

.handle:focus-visible {
  outline: 2px solid var(--color-primary);
}

.edit-form {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.35rem 0;
}

.edit-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.edit-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.4rem;
}

.edit-actions .delete {
  margin-right: auto;
  color: var(--color-danger);
}

.quick-add {
  display: flex;
  gap: 0.5rem;
}

.suggestions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
}

.suggestions-label {
  margin-right: 0.15rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-muted);
}

.suggestion {
  padding: 0.25rem 0.65rem;
  font: inherit;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border: 0;
  border-radius: 999px;
  cursor: pointer;
}

.suggestion:hover {
  color: #fff;
  background: var(--color-primary);
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}
</style>

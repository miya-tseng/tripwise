<script setup lang="ts">
import { computed, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { storeToRefs } from 'pinia'
import ChecklistSectionCard from '@/components/ChecklistSectionCard.vue'
import { useChecklistStore } from '@/stores/checklist'
import type { ChecklistSection } from '@/types/checklist'
import { SECTIONS, itemsIn, missingSuggestions, progressPercent } from '@/utils/checklist'
import { todayIso } from '@/utils/trips'

const props = withDefaults(defineProps<{ id: string; today?: string }>(), {
  today: () => todayIso(),
})

const store = useChecklistStore()
const { checklist, loading, loadError, error } = storeToRefs(store)

const tripLink = computed(() => ({ name: 'trip-schedule', params: { id: props.id } }))
const items = computed(() => checklist.value?.items ?? [])
const percent = computed(() =>
  progressPercent(checklist.value?.done ?? 0, checklist.value?.total ?? 0),
)
const packingSuggestions = computed(() => missingSuggestions(itemsIn(items.value, 'ToPack')))

watch(
  () => props.id,
  (id) => store.load(id),
  { immediate: true },
)

function add(section: ChecklistSection, text: string) {
  store.add({ text, section, dueDate: null })
}
</script>

<template>
  <main class="page">
    <RouterLink :to="tripLink" class="back-link" data-testid="back-to-trip">
      ← Back to the itinerary
    </RouterLink>

    <p v-if="loading" class="loading">Loading...</p>
    <div v-else-if="loadError" role="alert" class="alert" data-testid="checklist-load-error">
      {{ loadError }}
    </div>

    <template v-else-if="checklist">
      <header class="head">
        <span class="eyebrow">Before you go</span>
        <h1 class="display">Checklist</h1>
        <div class="progress-row">
          <div
            class="progress"
            role="progressbar"
            aria-label="Checklist progress"
            :aria-valuenow="checklist.done"
            aria-valuemin="0"
            :aria-valuemax="checklist.total"
          >
            <span class="progress-fill" :style="{ width: `${percent}%` }" />
          </div>
          <span class="progress-label" data-testid="checklist-progress">
            {{ checklist.done }} / {{ checklist.total }} done
          </span>
        </div>
      </header>

      <div v-if="error" role="alert" class="alert" data-testid="checklist-error">
        <span>{{ error }}</span>
        <button
          type="button"
          class="alert-close"
          aria-label="Dismiss"
          data-testid="dismiss-checklist-error"
          @click="store.clearError"
        >
          ✕
        </button>
      </div>

      <div class="sections">
        <ChecklistSectionCard
          v-for="section in SECTIONS"
          :key="section.value"
          :section="section"
          :items="itemsIn(items, section.value)"
          :bookings="section.value === 'ToBook' ? checklist.bookings : []"
          :suggestions="section.value === 'ToPack' ? packingSuggestions : []"
          :today="today"
          @add="(text) => add(section.value, text)"
          @toggle="store.toggle"
          @update="store.update"
          @remove="store.remove"
          @reorder="(ids) => store.reorder(section.value, ids)"
          @toggle-booking="store.toggleBooking"
        />
      </div>
    </template>
  </main>
</template>

<style scoped>
.page {
  max-width: 760px;
  margin: 0 auto;
  padding: 2rem clamp(1rem, 3vw, 2.5rem) 4rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.back-link {
  align-self: flex-start;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
}

.back-link:hover {
  text-decoration: underline;
}

.loading {
  color: var(--color-muted);
}

.head h1 {
  margin-top: 0.15rem;
  font-size: 2.2rem;
  line-height: 1.1;
}

.progress-row {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  margin-top: 0.75rem;
}

.progress {
  flex: 1;
  height: 0.55rem;
  overflow: hidden;
  background: rgb(31 92 86 / 0.12);
  border-radius: 999px;
}

.progress-fill {
  display: block;
  height: 100%;
  background: var(--color-primary);
  border-radius: inherit;
  transition: width 0.25s ease;
}

.progress-label {
  flex-shrink: 0;
  font-size: 0.9rem;
  font-weight: 700;
  color: var(--color-primary);
  font-variant-numeric: tabular-nums;
}

.sections {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
</style>

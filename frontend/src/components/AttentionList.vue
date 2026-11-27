<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { AttentionItem } from '@/types/overview'
import { attentionView } from '@/utils/overview'

const props = defineProps<{
  tripId: string
  items: AttentionItem[]
  startDate: string
}>()

const attention = computed(() =>
  props.items.map((item) => ({ item, view: attentionView(item, props.startDate) })),
)

function attentionLink(kind: string, dayNumber: number) {
  return kind === 'ChecklistOpen'
    ? { name: 'trip-checklist', params: { id: props.tripId } }
    : {
        name: 'trip-schedule',
        params: { id: props.tripId },
        query: { view: 'timeline', day: String(dayNumber) },
      }
}
</script>

<template>
  <div class="panel-content">
    <p v-if="attention.length === 0" class="all-set" data-testid="all-set">
      ✓ All set. Every night has a stay and nothing is waiting to be booked.
    </p>
    <ul v-else class="attention-list">
      <li
        v-for="entry in attention"
        :key="`${entry.item.kind}-${entry.item.date}-${entry.item.itemId}`"
      >
        <RouterLink
          :to="attentionLink(entry.item.kind, entry.view.dayNumber)"
          class="attention-item"
          :class="`kind-${entry.item.kind}`"
          data-testid="attention-item"
        >
          <span class="attention-icon" aria-hidden="true">{{ entry.view.icon }}</span>
          <span class="attention-text">
            <span class="attention-title">{{ entry.view.title }}</span>
            <span class="attention-detail">{{ entry.view.detail }}</span>
          </span>
          <span class="attention-go" aria-hidden="true">→</span>
        </RouterLink>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.panel-content {
  display: contents;
}

.all-set {
  font-size: 0.9rem;
  color: #1f5c56;
}

.attention-list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  max-height: 19rem;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  list-style: none;
}

.attention-item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 0.7rem;
  color: var(--color-text);
  text-decoration: none;
  background: var(--color-surface-muted);
  border: 1px solid transparent;
  border-radius: 10px;
}

.attention-item:hover,
.attention-item:focus-visible {
  border-color: var(--color-primary);
  outline: none;
}

.attention-item.kind-ActivityToBook,
.attention-item.kind-StayToBook {
  background: #fdf1e6;
}

.attention-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.attention-title {
  overflow: hidden;
  font-size: 0.88rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.attention-detail {
  font-size: 0.76rem;
  color: var(--color-muted);
}

.attention-go {
  color: var(--color-subtle);
}
</style>

<script setup lang="ts">
import type { Activity } from '@/types/activity'
import type { Suggestion } from '@/types/suggestion'
import { placeLink } from '@/utils/mapsLinks'

defineProps<{
  anchor: Activity
  suggestions: Suggestion[]
  loading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  close: []
  add: [suggestion: Suggestion]
}>()

function formatDuration(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60

  if (hours === 0) {
    return `${rest} min`
  }

  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`
}
</script>

<template>
  <section class="card suggestions" aria-live="polite" data-testid="suggestions-panel">
    <header class="suggestions-header">
      <div>
        <span class="eyebrow">Ideas</span>
        <h2 class="section-title">Near {{ anchor.name }}</h2>
      </div>
      <button
        type="button"
        class="btn btn-ghost btn-small"
        aria-label="Close ideas"
        data-testid="close-suggestions"
        @click="emit('close')"
      >
        ✕
      </button>
    </header>

    <p v-if="loading" class="message" data-testid="suggestions-loading">
      <span class="spinner" aria-hidden="true" />
      Finding places people usually pair with {{ anchor.name }}...
    </p>
    <p v-else-if="error" class="message error" role="alert" data-testid="suggestions-error">
      {{ error }}
    </p>
    <p v-else-if="suggestions.length === 0" class="message" data-testid="suggestions-empty">
      No new ideas for this place yet.
    </p>
    <ul v-else class="suggestion-list">
      <li
        v-for="suggestion in suggestions"
        :key="suggestion.place.placeId"
        class="suggestion"
        data-testid="suggestion"
      >
        <div class="suggestion-main">
          <span class="suggestion-name" data-testid="suggestion-name">{{
            suggestion.place.name
          }}</span>
          <span class="suggestion-address">{{ suggestion.place.address }}</span>
          <p class="suggestion-reason" data-testid="suggestion-reason">{{ suggestion.reason }}</p>
          <span class="suggestion-meta">
            <span class="chip" data-testid="suggestion-duration">
              ~{{ formatDuration(suggestion.suggestedMinutes) }}
            </span>
            <a
              :href="placeLink(suggestion.place)"
              target="_blank"
              rel="noopener noreferrer"
              class="maps-link"
              data-testid="suggestion-map-link"
            >
              Google Maps ↗
            </a>
          </span>
        </div>
        <button
          type="button"
          class="btn btn-secondary btn-small add"
          data-testid="add-suggestion"
          @click="emit('add', suggestion)"
        >
          Add to this day
        </button>
      </li>
    </ul>
    <p v-if="!loading && !error && suggestions.length > 0" class="ai-note">
      Suggested by AI, matched to real places on Google.
    </p>
  </section>
</template>

<style scoped>
.suggestions {
  padding: 1.4rem;
}

.suggestions-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.5rem;
}

.suggestions-header .section-title {
  margin-top: 0.2rem;
  overflow-wrap: anywhere;
}

.message {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-top: 1rem;
  font-size: 0.9rem;
  color: var(--color-muted);
}

.message.error {
  color: var(--color-danger);
}

.spinner {
  flex-shrink: 0;
  width: 1rem;
  height: 1rem;
  border: 2px solid var(--color-primary-soft);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.suggestion-list {
  margin: 1rem 0 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.suggestion {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  padding: 0.85rem 0.9rem;
  background: var(--color-surface-muted);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.suggestion-main {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.suggestion-name {
  font-weight: 700;
}

.suggestion-address {
  overflow: hidden;
  font-size: 0.78rem;
  color: var(--color-subtle);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.suggestion-reason {
  margin-top: 0.3rem;
  font-size: 0.88rem;
  color: var(--color-muted);
}

.suggestion-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 0.4rem;
}

.maps-link {
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--color-primary);
  text-decoration: none;
}

.maps-link:hover {
  text-decoration: underline;
}

.add {
  align-self: stretch;
}

.ai-note {
  margin-top: 0.8rem;
  font-size: 0.72rem;
  color: var(--color-subtle);
}
</style>

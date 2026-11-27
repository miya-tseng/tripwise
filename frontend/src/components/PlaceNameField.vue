<script setup lang="ts">
import PlaceSearch from '@/components/PlaceSearch.vue'
import type { GeoPoint, PlaceSuggestion } from '@/types/place'

export type PlaceNameMode = 'place' | 'custom'

withDefaults(
  defineProps<{
    label: string
    inputId: string
    inputName: string
    sessionToken: string
    maxlength: number
    customLabel: string
    customTestid: string
    placeTestid: string
    placeholder?: string
    near?: GeoPoint | null
    required?: boolean
  }>(),
  {
    placeholder: '',
    near: null,
    required: false,
  },
)

const mode = defineModel<PlaceNameMode>('mode', { required: true })
const place = defineModel<PlaceSuggestion | null>('place', { required: true })
const name = defineModel<string>('name', { required: true })

function useCustomName() {
  mode.value = 'custom'

  if (place.value && name.value.trim() === '') {
    name.value = place.value.name
  }
}
</script>

<template>
  <div class="field">
    <label class="field-label" :for="inputId">{{ label }}</label>
    <PlaceSearch
      v-if="mode === 'place'"
      v-model="place"
      :input-id="inputId"
      :session-token="sessionToken"
      :near="near"
    />
    <input
      v-else
      :id="inputId"
      v-model="name"
      class="text-input"
      :name="inputName"
      type="text"
      :placeholder="placeholder"
      autocomplete="off"
      :required="required"
      :maxlength="maxlength"
    />
    <button
      v-if="mode === 'place'"
      type="button"
      class="mode-switch"
      :data-testid="customTestid"
      @click="useCustomName"
    >
      {{ customLabel }}
    </button>
    <button
      v-else
      type="button"
      class="mode-switch"
      :data-testid="placeTestid"
      @click="mode = 'place'"
    >
      Search a real place instead
    </button>
  </div>
</template>

<style scoped>
.mode-switch {
  align-self: flex-start;
  padding: 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-primary);
  background: none;
  border: none;
  cursor: pointer;
}

.mode-switch:hover {
  text-decoration: underline;
}
</style>

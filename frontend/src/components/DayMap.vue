<script setup lang="ts">
import { onMounted, ref, shallowRef, watch } from 'vue'
import type { Place } from '@/types/place'
import {
  loadGoogleMaps,
  mapsMapId,
  type MapsApi,
  type MapsMap,
  type MapsMarker,
} from '@/utils/googleMaps'

export interface DayMapStop {
  activityId: string
  name: string
  place: Place
}

const props = withDefaults(
  defineProps<{
    stops: DayMapStop[]
    highlightedId?: string | null
  }>(),
  {
    highlightedId: null,
  },
)

const emit = defineEmits<{
  select: [activityId: string]
}>()

const container = ref<HTMLElement | null>(null)
const status = ref<'loading' | 'ready' | 'error'>('loading')
const api = shallowRef<MapsApi | null>(null)
const map = shallowRef<MapsMap | null>(null)
const markers = new Map<string, MapsMarker>()

const PRIMARY = '#1f5c56'
const ACCENT = '#c9714b'

function pinFor(maps: MapsApi, number: number, highlighted: boolean): HTMLElement {
  return new maps.PinElement({
    glyph: String(number),
    background: highlighted ? ACCENT : PRIMARY,
    borderColor: '#ffffff',
    glyphColor: '#ffffff',
    scale: highlighted ? 1.25 : 1,
  }).element
}

function renderMarkers() {
  const maps = api.value
  const currentMap = map.value

  if (!maps || !currentMap) {
    return
  }

  markers.forEach((marker) => (marker.map = null))
  markers.clear()

  const bounds = new maps.LatLngBounds()

  props.stops.forEach((stop, index) => {
    const position = { lat: stop.place.latitude, lng: stop.place.longitude }
    const highlighted = stop.activityId === props.highlightedId
    const marker = new maps.AdvancedMarkerElement({
      map: currentMap,
      position,
      title: `${index + 1}. ${stop.name}`,
      content: pinFor(maps, index + 1, highlighted),
      gmpClickable: true,
    })
    marker.zIndex = highlighted ? 10 : index
    marker.addListener('click', () => emit('select', stop.activityId))
    markers.set(stop.activityId, marker)
    bounds.extend(position)
  })

  if (props.stops.length === 1) {
    const only = props.stops[0]!.place
    currentMap.setCenter({ lat: only.latitude, lng: only.longitude })
    currentMap.setZoom(15)
  } else if (props.stops.length > 1) {
    currentMap.fitBounds(bounds, 48)
  }
}

function highlight(activityId: string | null) {
  const maps = api.value

  if (!maps) {
    return
  }

  props.stops.forEach((stop, index) => {
    const marker = markers.get(stop.activityId)

    if (marker) {
      const highlighted = stop.activityId === activityId
      marker.content = pinFor(maps, index + 1, highlighted)
      marker.zIndex = highlighted ? 10 : index
    }
  })

  const stop = props.stops.find((item) => item.activityId === activityId)

  if (stop && map.value) {
    map.value.panTo({ lat: stop.place.latitude, lng: stop.place.longitude })
  }
}

onMounted(async () => {
  try {
    const maps = await loadGoogleMaps()

    if (!container.value) {
      return
    }

    api.value = maps
    map.value = new maps.Map(container.value, {
      mapId: mapsMapId(),
      disableDefaultUI: true,
      zoomControl: true,
      clickableIcons: false,
      gestureHandling: 'cooperative',
    })
    status.value = 'ready'
    renderMarkers()
  } catch {
    status.value = 'error'
  }
})

watch(() => props.stops, renderMarkers, { deep: true })
watch(() => props.highlightedId, highlight)
</script>

<template>
  <div class="day-map" data-testid="day-map">
    <div ref="container" class="map-canvas" role="region" aria-label="Map of this day" />
    <p v-if="status === 'loading'" class="map-message" data-testid="day-map-loading">
      Loading map...
    </p>
    <p
      v-else-if="status === 'error'"
      class="map-message error"
      role="alert"
      data-testid="day-map-error"
    >
      The map could not be loaded. The Google Maps links still work.
    </p>
  </div>
</template>

<style scoped>
.day-map {
  position: relative;
  margin: 1rem 0 0 4.4rem;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-surface-muted);
}

.map-canvas {
  width: 100%;
  height: 16rem;
}

.map-message {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  font-size: 0.9rem;
  text-align: center;
  color: var(--color-muted);
}

.map-message.error {
  color: var(--color-danger);
}

@media (max-width: 560px) {
  .day-map {
    margin-left: 0;
  }
}
</style>

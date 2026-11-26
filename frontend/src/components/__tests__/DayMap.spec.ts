import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { loadGoogleMaps } from '@/utils/googleMaps'
import type { Place } from '@/types/place'
import { activeMarkers, FakeMap, fakeMapsApi } from '@/test/fakeGoogleMaps'
import DayMap, { type DayMapStop } from '../DayMap.vue'

vi.mock('@/utils/googleMaps', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/utils/googleMaps')>()),
  loadGoogleMaps: vi.fn<() => Promise<import('@/utils/googleMaps').MapsApi>>(),
  mapsMapId: () => 'TEST_MAP_ID',
}))

function place(id: string, latitude: number, longitude: number): Place {
  return {
    placeId: id,
    name: id,
    address: '',
    latitude,
    longitude,
    googleMapsUri: `https://maps.google.com/?cid=${id}`,
  }
}

const stops: DayMapStop[] = [
  { activityId: 'a', name: 'Senso-ji', place: place('senso-ji', 35.71, 139.79) },
  { activityId: 'b', name: 'Tokyo Skytree', place: place('skytree', 35.71, 139.81) },
  { activityId: 'c', name: 'Ueno Park', place: place('ueno', 35.71, 139.77) },
]

async function mountMap(mapStops = stops, highlightedId: string | null = null) {
  const wrapper = mount(DayMap, { props: { stops: mapStops, highlightedId } })
  await flushPromises()
  return wrapper
}

describe('DayMap', () => {
  beforeEach(() => {
    vi.mocked(loadGoogleMaps).mockResolvedValue(fakeMapsApi())
  })

  it('shows a loading message until the map is ready', async () => {
    vi.mocked(loadGoogleMaps).mockReturnValue(new Promise(() => {}))
    const wrapper = mount(DayMap, { props: { stops } })

    expect(wrapper.find('[data-testid="day-map-loading"]').text()).toBe('Loading map...')
  })

  it('creates a map with the configured map id', async () => {
    await mountMap()

    expect(FakeMap.instances).toHaveLength(1)
    expect(FakeMap.instances[0]!.options).toMatchObject({ mapId: 'TEST_MAP_ID' })
  })

  it('adds numbered markers in schedule order', async () => {
    await mountMap()

    expect(activeMarkers().map((marker) => marker.glyph)).toEqual(['1', '2', '3'])
    expect(activeMarkers().map((marker) => marker.title)).toEqual([
      '1. Senso-ji',
      '2. Tokyo Skytree',
      '3. Ueno Park',
    ])
    expect(activeMarkers()[1]!.position).toEqual({ lat: 35.71, lng: 139.81 })
  })

  it('fits the map around every stop', async () => {
    await mountMap()

    expect(FakeMap.instances[0]!.fittedTo).toEqual(
      stops.map((stop) => ({ lat: stop.place.latitude, lng: stop.place.longitude })),
    )
  })

  it('centers on a single stop', async () => {
    await mountMap([stops[0]!])

    expect(FakeMap.instances[0]!.center).toEqual({ lat: 35.71, lng: 139.79 })
    expect(FakeMap.instances[0]!.zoom).toBe(15)
  })

  it('emits the activity when a marker is clicked', async () => {
    const wrapper = await mountMap()

    activeMarkers()[2]!.click()

    expect(wrapper.emitted('select')).toEqual([['c']])
  })

  it('highlights the marker of the highlighted activity and pans to it', async () => {
    const wrapper = await mountMap()

    await wrapper.setProps({ highlightedId: 'b' })

    expect(activeMarkers().map((marker) => marker.highlighted)).toEqual([false, true, false])
    expect(FakeMap.instances[0]!.pannedTo).toEqual([{ lat: 35.71, lng: 139.81 }])
  })

  it('redraws markers when the stops change', async () => {
    const wrapper = await mountMap()

    await wrapper.setProps({ stops: [stops[2]!, stops[0]!] })
    await flushPromises()

    expect(activeMarkers().map((marker) => marker.title)).toEqual(['1. Ueno Park', '2. Senso-ji'])
  })

  it('explains when the map cannot be loaded', async () => {
    vi.mocked(loadGoogleMaps).mockRejectedValue(new Error('blocked'))

    const wrapper = await mountMap()

    expect(wrapper.find('[data-testid="day-map-error"]').text()).toBe(
      'The map could not be loaded. The Google Maps links still work.',
    )
  })
})

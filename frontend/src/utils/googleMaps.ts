export interface LatLngLiteral {
  lat: number
  lng: number
}

export interface MapsMap {
  fitBounds(bounds: MapsLatLngBounds, padding?: number): void
  setCenter(center: LatLngLiteral): void
  setZoom(zoom: number): void
  panTo(center: LatLngLiteral): void
}

export interface MapsLatLngBounds {
  extend(point: LatLngLiteral): void
}

export interface MapsPin {
  element: HTMLElement
}

export interface MapsMarker {
  map: MapsMap | null
  content: HTMLElement | null
  zIndex: number | null
  addListener(event: 'click', handler: () => void): void
}

export interface MapsApi {
  Map: new (element: HTMLElement, options: Record<string, unknown>) => MapsMap
  LatLngBounds: new () => MapsLatLngBounds
  AdvancedMarkerElement: new (options: {
    map: MapsMap
    position: LatLngLiteral
    title: string
    content: HTMLElement
    gmpClickable: boolean
  }) => MapsMarker
  PinElement: new (options: {
    glyph: string
    background: string
    borderColor: string
    glyphColor: string
    scale: number
  }) => MapsPin
}

interface GoogleMapsGlobal {
  maps: {
    importLibrary(name: string): Promise<Record<string, unknown>>
  }
}

const SCRIPT_ID = 'google-maps-js'
const CALLBACK_NAME = '__tripwiseGoogleMapsReady'

let loading: Promise<MapsApi> | null = null

export function mapsBrowserKey(): string {
  return import.meta.env.VITE_GOOGLE_MAPS_BROWSER_KEY?.trim() ?? ''
}

export function mapsMapId(): string {
  return import.meta.env.VITE_GOOGLE_MAPS_MAP_ID?.trim() || 'DEMO_MAP_ID'
}

export function hasMapsKey(): boolean {
  return mapsBrowserKey() !== ''
}

function injectScript(key: string): Promise<GoogleMapsGlobal> {
  return new Promise((resolve, reject) => {
    const globalScope = window as unknown as Record<string, unknown>
    globalScope[CALLBACK_NAME] = () => resolve(globalScope.google as GoogleMapsGlobal)

    const params = new URLSearchParams({
      key,
      v: 'weekly',
      loading: 'async',
      callback: CALLBACK_NAME,
    })
    const script = document.createElement('script')
    script.id = SCRIPT_ID
    script.async = true
    script.src = `https://maps.googleapis.com/maps/api/js?${params}`
    script.onerror = () => reject(new Error('Google Maps could not be loaded.'))
    document.head.appendChild(script)
  })
}

export function loadGoogleMaps(): Promise<MapsApi> {
  if (!loading) {
    loading = injectScript(mapsBrowserKey())
      .then(async (google) => {
        const [maps, marker, core] = await Promise.all([
          google.maps.importLibrary('maps'),
          google.maps.importLibrary('marker'),
          google.maps.importLibrary('core'),
        ])
        return { ...core, ...maps, ...marker } as unknown as MapsApi
      })
      .catch((error: unknown) => {
        loading = null
        document.getElementById(SCRIPT_ID)?.remove()
        throw error
      })
  }

  return loading
}

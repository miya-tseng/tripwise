import type {
  LatLngLiteral,
  MapsApi,
  MapsLatLngBounds,
  MapsMap,
  MapsMarker,
  MapsPin,
} from '@/utils/googleMaps'

export class FakeMap implements MapsMap {
  static instances: FakeMap[] = []
  fittedTo: LatLngLiteral[] | null = null
  center: LatLngLiteral | null = null
  zoom: number | null = null
  pannedTo: LatLngLiteral[] = []

  constructor(
    public element: HTMLElement,
    public options: Record<string, unknown>,
  ) {
    FakeMap.instances.push(this)
  }

  fitBounds(bounds: MapsLatLngBounds) {
    this.fittedTo = (bounds as FakeBounds).points
  }

  setCenter(center: LatLngLiteral) {
    this.center = center
  }

  setZoom(zoom: number) {
    this.zoom = zoom
  }

  panTo(center: LatLngLiteral) {
    this.pannedTo.push(center)
  }
}

export class FakeBounds implements MapsLatLngBounds {
  points: LatLngLiteral[] = []

  extend(point: LatLngLiteral) {
    this.points.push(point)
  }
}

export class FakePin implements MapsPin {
  element: HTMLElement

  constructor(public options: { glyph: string; background: string; scale: number }) {
    this.element = document.createElement('div')
    this.element.dataset.glyph = options.glyph
    this.element.dataset.background = options.background
    this.element.dataset.scale = String(options.scale)
  }
}

export class FakeMarker implements MapsMarker {
  static instances: FakeMarker[] = []
  map: MapsMap | null
  content: HTMLElement | null
  zIndex: number | null = null
  position: LatLngLiteral
  title: string
  private clickHandler: (() => void) | null = null

  constructor(options: {
    map: MapsMap
    position: LatLngLiteral
    title: string
    content: HTMLElement
  }) {
    this.map = options.map
    this.position = options.position
    this.title = options.title
    this.content = options.content
    FakeMarker.instances.push(this)
  }

  addListener(_event: 'click', handler: () => void) {
    this.clickHandler = handler
  }

  click() {
    this.clickHandler?.()
  }

  get glyph() {
    return this.content?.dataset.glyph
  }

  get highlighted() {
    return this.content?.dataset.scale !== '1'
  }
}

export function fakeMapsApi(): MapsApi {
  FakeMap.instances = []
  FakeMarker.instances = []

  return {
    Map: FakeMap,
    LatLngBounds: FakeBounds,
    AdvancedMarkerElement: FakeMarker,
    PinElement: FakePin,
  } as unknown as MapsApi
}

export function activeMarkers(): FakeMarker[] {
  return FakeMarker.instances.filter((marker) => marker.map !== null)
}

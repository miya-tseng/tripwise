import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

async function freshModule() {
  vi.resetModules()
  return import('../googleMaps')
}

function injectedScript(): HTMLScriptElement | null {
  return document.getElementById('google-maps-js') as HTMLScriptElement | null
}

describe('googleMaps', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_GOOGLE_MAPS_BROWSER_KEY', 'browser-key')
  })

  afterEach(() => {
    vi.unstubAllEnvs()
    injectedScript()?.remove()
    delete (window as unknown as Record<string, unknown>).google
  })

  it('knows whether a browser key is configured', async () => {
    expect((await freshModule()).hasMapsKey()).toBe(true)

    vi.stubEnv('VITE_GOOGLE_MAPS_BROWSER_KEY', '  ')
    expect((await freshModule()).hasMapsKey()).toBe(false)
  })

  it('uses the demo map id unless one is configured', async () => {
    expect((await freshModule()).mapsMapId()).toBe('DEMO_MAP_ID')

    vi.stubEnv('VITE_GOOGLE_MAPS_MAP_ID', 'my-map')
    expect((await freshModule()).mapsMapId()).toBe('my-map')
  })

  it('loads the script once with the key and returns the libraries', async () => {
    const { loadGoogleMaps } = await freshModule()
    const importLibrary = vi.fn<(name: string) => Promise<Record<string, string>>>(
      async (name) => ({
        [`${name}Library`]: name,
      }),
    )

    const first = loadGoogleMaps()
    const second = loadGoogleMaps()
    const script = injectedScript()!
    const url = new URL(script.src)
    ;(window as unknown as Record<string, unknown>).google = { maps: { importLibrary } }
    ;(window as unknown as Record<string, () => void>).__tripwiseGoogleMapsReady!()

    const api = await first
    expect(await second).toBe(api)
    expect(url.origin + url.pathname).toBe('https://maps.googleapis.com/maps/api/js')
    expect(url.searchParams.get('key')).toBe('browser-key')
    expect(url.searchParams.get('loading')).toBe('async')
    expect(importLibrary.mock.calls.map(([name]) => name).sort()).toEqual([
      'core',
      'maps',
      'marker',
    ])
    expect(api).toMatchObject({ mapsLibrary: 'maps', markerLibrary: 'marker', coreLibrary: 'core' })
    expect(document.querySelectorAll('#google-maps-js')).toHaveLength(1)
  })

  it('removes the script and can retry after a load error', async () => {
    const { loadGoogleMaps } = await freshModule()

    const failed = loadGoogleMaps()
    injectedScript()!.onerror!(new Event('error'))

    await expect(failed).rejects.toThrow('Google Maps could not be loaded.')
    expect(injectedScript()).toBeNull()

    loadGoogleMaps()
    expect(injectedScript()).not.toBeNull()
  })
})

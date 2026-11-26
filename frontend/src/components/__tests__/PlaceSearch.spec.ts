import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { searchPlaces } from '@/api/places'
import { ApiError } from '@/api/http'
import type { PlaceSuggestion } from '@/types/place'
import PlaceSearch from '../PlaceSearch.vue'

vi.mock('@/api/places')

const sensoji: PlaceSuggestion = {
  placeId: 'senso-ji-id',
  name: 'Senso-ji',
  description: 'Asakusa, Taito City, Tokyo',
}
const skytree: PlaceSuggestion = {
  placeId: 'skytree-id',
  name: 'Tokyo Skytree',
  description: 'Oshiage, Sumida City, Tokyo',
}

let wrapper: VueWrapper | null = null

function mountSearch(
  modelValue: PlaceSuggestion | null = null,
  near = { latitude: 35.7, longitude: 139.8 },
) {
  wrapper = mount(PlaceSearch, {
    props: {
      modelValue,
      sessionToken: 'session-1',
      near,
      debounceMs: 200,
      'onUpdate:modelValue': (value: PlaceSuggestion | null) =>
        wrapper!.setProps({ modelValue: value }),
    },
    attachTo: document.body,
  })
  return wrapper
}

function input(search: VueWrapper) {
  return search.find('[data-testid="place-search-input"]')
}

async function type(search: VueWrapper, text: string) {
  await input(search).setValue(text)
}

async function settle() {
  await vi.advanceTimersByTimeAsync(200)
  await flushPromises()
}

function optionNames(search: VueWrapper): string[] {
  return search.findAll('[data-testid="place-option"] .option-name').map((option) => option.text())
}

describe('PlaceSearch', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.resetAllMocks()
    vi.mocked(searchPlaces).mockResolvedValue([sensoji, skytree])
  })

  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
    vi.useRealTimers()
  })

  it('does not search for fewer than two characters', async () => {
    const search = mountSearch()

    await type(search, 's')
    await settle()

    expect(searchPlaces).not.toHaveBeenCalled()
    expect(search.find('[data-testid="place-panel"]').isVisible()).toBe(false)
  })

  it('debounces typing into a single search with the session and location', async () => {
    const search = mountSearch()

    await type(search, 'se')
    await type(search, 'sen')
    await type(search, ' senso ')
    await settle()

    expect(searchPlaces).toHaveBeenCalledTimes(1)
    expect(searchPlaces).toHaveBeenCalledWith('senso', 'session-1', {
      latitude: 35.7,
      longitude: 139.8,
    })
  })

  it('shows the suggestions with the first one active', async () => {
    const search = mountSearch()

    await type(search, 'tokyo')
    await settle()

    expect(optionNames(search)).toEqual(['Senso-ji', 'Tokyo Skytree'])
    expect(search.find('[data-testid="place-option"]').text()).toContain(
      'Asakusa, Taito City, Tokyo',
    )
    expect(input(search).attributes('aria-expanded')).toBe('true')
    const activeId = input(search).attributes('aria-activedescendant')
    expect(search.find(`#${CSS.escape(activeId!)}`).text()).toContain('Senso-ji')
  })

  it('selects a suggestion with the keyboard', async () => {
    const search = mountSearch()

    await type(search, 'tokyo')
    await settle()
    await input(search).trigger('keydown', { key: 'ArrowDown' })
    await input(search).trigger('keydown', { key: 'Enter' })

    expect(search.emitted('update:modelValue')).toEqual([[skytree]])
  })

  it('selects a suggestion with a click', async () => {
    const search = mountSearch()

    await type(search, 'tokyo')
    await settle()
    await search.findAll('[data-testid="place-option"]')[0]!.trigger('click')

    expect(search.emitted('update:modelValue')).toEqual([[sensoji]])
  })

  it('shows the selected place with a change button', async () => {
    const search = mountSearch(sensoji)

    expect(search.find('[data-testid="selected-place-name"]').text()).toBe('Senso-ji')
    expect(search.find('[data-testid="selected-place"]').text()).toContain(
      'Asakusa, Taito City, Tokyo',
    )
    expect(input(search).exists()).toBe(false)

    await search.find('[data-testid="change-place"]').trigger('click')
    await flushPromises()

    expect(search.emitted('update:modelValue')).toEqual([[null]])
    expect(document.activeElement).toBe(input(search).element)
  })

  it('says when nothing is found', async () => {
    vi.mocked(searchPlaces).mockResolvedValue([])
    const search = mountSearch()

    await type(search, 'zzzz')
    await settle()

    expect(search.find('[data-testid="place-search-empty"]').text()).toBe('No places found.')
  })

  it('shows the error when place search fails', async () => {
    vi.mocked(searchPlaces).mockRejectedValue(
      new ApiError('Place search is unavailable right now. Please try again later.', 502),
    )
    const search = mountSearch()

    await type(search, 'senso')
    await settle()

    expect(search.find('[data-testid="place-search-error"]').text()).toBe(
      'Place search is unavailable right now. Please try again later.',
    )
  })

  it('ignores results that arrive after a newer search', async () => {
    let resolveSlow: (value: PlaceSuggestion[]) => void = () => {}
    vi.mocked(searchPlaces)
      .mockImplementationOnce(() => new Promise((resolve) => (resolveSlow = resolve)))
      .mockResolvedValueOnce([skytree])
    const search = mountSearch()

    await type(search, 'senso')
    await settle()
    await type(search, 'skytree')
    await settle()
    resolveSlow([sensoji])
    await flushPromises()

    expect(optionNames(search)).toEqual(['Tokyo Skytree'])
  })

  it('closes the suggestions with escape', async () => {
    const search = mountSearch()

    await type(search, 'tokyo')
    await settle()
    await input(search).trigger('keydown', { key: 'Escape' })

    expect(input(search).attributes('aria-expanded')).toBe('false')
  })

  it('credits Google in the suggestions', async () => {
    const search = mountSearch()

    await type(search, 'tokyo')
    await settle()

    expect(search.find('.powered-by').text()).toBe('Powered by Google')
  })
})

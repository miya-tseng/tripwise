import { afterEach, describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import PlaceNameField, { type PlaceNameMode } from '../PlaceNameField.vue'
import type { PlaceSuggestion } from '@/types/place'

let wrapper: VueWrapper | null = null

const tokyoTower: PlaceSuggestion = {
  placeId: 'tower',
  name: 'Tokyo Tower',
  description: 'Minato City',
}

function mountField(mode: PlaceNameMode, place: PlaceSuggestion | null = null, name = '') {
  wrapper = mount(PlaceNameField, {
    props: {
      label: 'Activity',
      inputId: 'activity',
      inputName: 'name',
      sessionToken: 'token',
      maxlength: 80,
      customLabel: 'Not a place? Add a custom activity',
      customTestid: 'use-custom',
      placeTestid: 'use-place',
      mode,
      place,
      name,
      'onUpdate:mode': (value: PlaceNameMode) => wrapper!.setProps({ mode: value }),
      'onUpdate:name': (value: string) => wrapper!.setProps({ name: value }),
    },
    global: { stubs: { PlaceSearch: true } },
  })
  return wrapper
}

describe('PlaceNameField', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('shows place search with a switch to a custom name', () => {
    const field = mountField('place')

    expect(field.findComponent({ name: 'PlaceSearch' }).exists()).toBe(true)
    expect(field.find('input[name="name"]').exists()).toBe(false)
    expect(field.get('[data-testid="use-custom"]').text()).toBe(
      'Not a place? Add a custom activity',
    )
  })

  it('switches to a custom name and keeps the picked place name', async () => {
    const field = mountField('place', tokyoTower)

    await field.get('[data-testid="use-custom"]').trigger('click')

    expect(field.emitted('update:mode')).toEqual([['custom']])
    expect(field.emitted('update:name')).toEqual([['Tokyo Tower']])
    expect((field.get('input[name="name"]').element as HTMLInputElement).value).toBe('Tokyo Tower')
  })

  it('keeps a name that was already typed', async () => {
    const field = mountField('place', tokyoTower, 'Lunch break')

    await field.get('[data-testid="use-custom"]').trigger('click')

    expect(field.emitted('update:name')).toBeUndefined()
  })

  it('switches back to place search', async () => {
    const field = mountField('custom')

    await field.get('[data-testid="use-place"]').trigger('click')

    expect(field.emitted('update:mode')).toEqual([['place']])
  })

  it('labels the custom name input', () => {
    const field = mountField('custom')

    expect(field.get('label').attributes('for')).toBe('activity')
    expect(field.get('input').attributes('id')).toBe('activity')
    expect(field.get('input').attributes('maxlength')).toBe('80')
  })
})

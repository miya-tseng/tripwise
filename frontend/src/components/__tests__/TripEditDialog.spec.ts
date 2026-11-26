import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import PlaceSearch from '../PlaceSearch.vue'
import TripEditDialog from '../TripEditDialog.vue'
import type { Trip, UpdateTripInput } from '@/types/trip'

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-05',
  days: 5,
}

type Submit = (input: UpdateTripInput) => Promise<boolean>

let wrapper: VueWrapper | null = null

function mountDialog(
  submit: Submit = vi.fn<Submit>().mockResolvedValue(true),
  error: string | null = null,
) {
  wrapper = mount(TripEditDialog, {
    props: { open: true, trip: tokyo, submit, error },
    attachTo: document.body,
  })
  return wrapper
}

function nameInput(dialog: VueWrapper) {
  return dialog.find('input[name="edit-name"]')
}

function saveButton(dialog: VueWrapper) {
  return dialog.find('button[type="submit"]')
}

const tokyoCity = {
  placeId: 'city-tokyo',
  name: 'Tokyo',
  address: 'Japan',
  latitude: 35.68,
  longitude: 139.65,
  googleMapsUri: 'https://maps.google.com/?cid=1',
}

describe('TripEditDialog', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('is hidden when closed', () => {
    wrapper = mount(TripEditDialog, {
      props: { open: false, trip: tokyo, submit: vi.fn<Submit>() },
    })

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('prefills the name and dates of the trip and focuses the name', async () => {
    const dialog = mountDialog()
    await flushPromises()

    expect((nameInput(dialog).element as HTMLInputElement).value).toBe('Tokyo')
    expect(dialog.findComponent(VueDatePicker).props('modelValue')).toEqual([
      '2026-12-01',
      '2026-12-05',
    ])
    expect(dialog.find('[data-testid="edit-dates-hint"]').text()).toBe('5 days')
    expect(document.activeElement).toBe(nameInput(dialog).element)
  })

  it('submits the new name and dates and closes after saving', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const dialog = mountDialog(submit)

    await nameInput(dialog).setValue('Osaka')
    dialog.findComponent(VueDatePicker).vm.$emit('update:model-value', ['2026-12-02', '2026-12-04'])
    await flushPromises()
    expect(dialog.find('[data-testid="edit-dates-hint"]').text()).toBe('3 days')

    await dialog.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith({
      name: 'Osaka',
      startDate: '2026-12-02',
      endDate: '2026-12-04',
      destinationPlaceId: null,
    })
    expect(dialog.emitted('close')).toHaveLength(1)
  })

  it('stays open when saving fails', async () => {
    const dialog = mountDialog(vi.fn<Submit>().mockResolvedValue(false))

    await dialog.find('form').trigger('submit')
    await flushPromises()

    expect(dialog.emitted('close')).toBeUndefined()
  })

  it('shows the error from the server', () => {
    const detail = '1 activity falls outside the new dates. Move or delete it first.'
    const dialog = mountDialog(undefined, detail)

    expect(dialog.find('[data-testid="edit-trip-error"]').text()).toBe(detail)
  })

  it('disables save without a name', async () => {
    const dialog = mountDialog()

    await nameInput(dialog).setValue('   ')

    expect(saveButton(dialog).attributes('disabled')).toBeDefined()
  })

  it('limits the date range to the maximum trip length', () => {
    const dialog = mountDialog()

    expect(dialog.findComponent(VueDatePicker).props('range')).toMatchObject({ maxRange: 29 })
  })

  it.each([
    [
      'cancel button',
      (dialog: VueWrapper) => dialog.find('[data-testid="cancel-edit-trip"]').trigger('click'),
    ],
    [
      'escape key',
      (dialog: VueWrapper) => dialog.find('form').trigger('keydown', { key: 'Escape' }),
    ],
    [
      'backdrop click',
      (dialog: VueWrapper) => dialog.find('[data-testid="edit-trip-backdrop"]').trigger('click'),
    ],
  ])('closes with the %s', async (_, close) => {
    const dialog = mountDialog()

    await close(dialog)

    expect(dialog.emitted('close')).toHaveLength(1)
  })

  it('resets the fields when it opens again', async () => {
    const dialog = mountDialog()
    await nameInput(dialog).setValue('Draft name')

    await dialog.setProps({ open: false })
    await dialog.setProps({ open: true })
    await flushPromises()

    expect((nameInput(dialog).element as HTMLInputElement).value).toBe('Tokyo')
  })

  it('shows the current destination and keeps it when saving', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    wrapper = mount(TripEditDialog, {
      props: { open: true, trip: { ...tokyo, destination: tokyoCity }, submit },
      attachTo: document.body,
    })
    await flushPromises()

    expect(wrapper.find('[data-testid="selected-place-name"]').text()).toBe('Tokyo')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({ destinationPlaceId: 'city-tokyo' }),
    )
  })

  it('clears the destination when it is removed', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    wrapper = mount(TripEditDialog, {
      props: { open: true, trip: { ...tokyo, destination: tokyoCity }, submit },
      attachTo: document.body,
    })
    await flushPromises()

    wrapper.findComponent(PlaceSearch).vm.$emit('update:modelValue', null)
    await flushPromises()
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith(expect.objectContaining({ destinationPlaceId: null }))
  })
})

import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import NewTripDialog from '../NewTripDialog.vue'
import PlaceSearch from '../PlaceSearch.vue'
import type { PlaceSuggestion } from '@/types/place'
import type { CreateTripInput } from '@/types/trip'

type Submit = (input: CreateTripInput) => Promise<boolean>

const tokyo: PlaceSuggestion = { placeId: 'city-tokyo', name: 'Tokyo', description: 'Japan' }
const kyoto: PlaceSuggestion = { placeId: 'city-kyoto', name: 'Kyoto', description: 'Japan' }

let wrapper: VueWrapper | null = null

function mountDialog(
  submit: Submit = vi.fn<Submit>().mockResolvedValue(true),
  error: string | null = null,
) {
  wrapper = mount(NewTripDialog, {
    props: { open: true, submit, error },
    attachTo: document.body,
  })
  return wrapper
}

function nameInput(dialog: VueWrapper) {
  return dialog.find<HTMLInputElement>('input[name="name"]')
}

async function chooseDestination(dialog: VueWrapper, place: PlaceSuggestion | null) {
  dialog.findComponent(PlaceSearch).vm.$emit('update:modelValue', place)
  await flushPromises()
}

async function chooseDates(dialog: VueWrapper, dates: string[] | null) {
  dialog.findComponent(VueDatePicker).vm.$emit('update:model-value', dates)
  await flushPromises()
}

async function submitForm(dialog: VueWrapper) {
  await dialog.find('form').trigger('submit')
  await flushPromises()
}

describe('NewTripDialog', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('is hidden when closed', () => {
    wrapper = mount(NewTripDialog, { props: { open: false, submit: vi.fn<Submit>() } })

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('searches destinations only and focuses the search', async () => {
    const dialog = mountDialog()
    await flushPromises()

    expect(dialog.findComponent(PlaceSearch).props('kind')).toBe('destination')
    expect(document.activeElement).toBe(dialog.find('[data-testid="place-search-input"]').element)
  })

  it('fills the name from the chosen destination', async () => {
    const dialog = mountDialog()

    await chooseDestination(dialog, tokyo)

    expect(nameInput(dialog).element.value).toBe('Tokyo')
    expect(dialog.find('[data-testid="name-hint"]').text()).toBe('From the destination')
  })

  it('follows a new destination while the name is still the automatic one', async () => {
    const dialog = mountDialog()

    await chooseDestination(dialog, tokyo)
    await chooseDestination(dialog, kyoto)

    expect(nameInput(dialog).element.value).toBe('Kyoto')
  })

  it('keeps a name the traveler typed', async () => {
    const dialog = mountDialog()

    await nameInput(dialog).setValue('Honeymoon')
    await chooseDestination(dialog, tokyo)

    expect(nameInput(dialog).element.value).toBe('Honeymoon')
    expect(dialog.find('[data-testid="name-hint"]').text()).toBe('')
  })

  it('submits the destination with its session and closes', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const dialog = mountDialog(submit)

    await chooseDestination(dialog, tokyo)
    await chooseDates(dialog, ['2026-12-01', '2026-12-05'])
    expect(dialog.find('[data-testid="dates-hint"]').text()).toBe('5 days')
    await submitForm(dialog)

    expect(submit).toHaveBeenCalledWith({
      name: 'Tokyo',
      startDate: '2026-12-01',
      endDate: '2026-12-05',
      destinationPlaceId: 'city-tokyo',
      placeSessionToken: expect.any(String),
    })
    expect(dialog.emitted('close')).toHaveLength(1)
  })

  it('creates a trip without a destination', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const dialog = mountDialog(submit)

    await nameInput(dialog).setValue('Weekend away')
    await chooseDates(dialog, ['2026-12-01', '2026-12-02'])
    await submitForm(dialog)

    expect(submit).toHaveBeenCalledWith({
      name: 'Weekend away',
      startDate: '2026-12-01',
      endDate: '2026-12-02',
    })
  })

  it('cannot be submitted without a name or dates', async () => {
    const submit = vi.fn<Submit>()
    const dialog = mountDialog(submit)
    const create = dialog.find('[data-testid="create-trip"]')

    expect(create.attributes('disabled')).toBeDefined()
    await chooseDestination(dialog, tokyo)
    expect(create.attributes('disabled')).toBeDefined()
    await chooseDates(dialog, ['2026-12-01', '2026-12-02'])
    expect(create.attributes('disabled')).toBeUndefined()
  })

  it('stays open with the values when creating fails', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(false)
    const dialog = mountDialog(submit)

    await chooseDestination(dialog, tokyo)
    await chooseDates(dialog, ['2026-12-01', '2026-12-02'])
    await submitForm(dialog)

    expect(dialog.emitted('close')).toBeUndefined()
    expect(nameInput(dialog).element.value).toBe('Tokyo')
  })

  it('shows the error', () => {
    const dialog = mountDialog(undefined, 'Place not found.')

    expect(dialog.find('[data-testid="new-trip-error"]').text()).toBe('Place not found.')
  })

  it('starts empty each time it opens', async () => {
    const dialog = mountDialog()
    await chooseDestination(dialog, tokyo)

    await dialog.setProps({ open: false })
    await dialog.setProps({ open: true })
    await flushPromises()

    expect(nameInput(dialog).element.value).toBe('')
    expect(dialog.findComponent(PlaceSearch).props('modelValue')).toBeNull()
  })

  it('closes on cancel', async () => {
    const dialog = mountDialog()

    await dialog.find('[data-testid="cancel-new-trip"]').trigger('click')

    expect(dialog.emitted('close')).toHaveLength(1)
  })
})

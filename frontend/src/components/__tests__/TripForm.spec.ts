import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import TripForm from '../TripForm.vue'
import type { CreateTripInput } from '@/types/trip'

type Submit = (input: CreateTripInput) => Promise<boolean>

function mountForm(submit: Submit) {
  return mount(TripForm, { props: { submit } })
}

async function fill(wrapper: VueWrapper, name: string, dates: string[] | null) {
  await wrapper.find('input[name="name"]').setValue(name)
  wrapper.findComponent(VueDatePicker).vm.$emit('update:model-value', dates)
  await flushPromises()
}

async function submitForm(wrapper: VueWrapper) {
  await wrapper.find('form').trigger('submit')
  await flushPromises()
}

describe('TripForm', () => {
  it('submits the name and the selected date range', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit)

    await fill(wrapper, 'Tokyo', ['2026-12-01', '2026-12-05'])
    await submitForm(wrapper)

    expect(submit).toHaveBeenCalledWith({
      name: 'Tokyo',
      startDate: '2026-12-01',
      endDate: '2026-12-05',
    })
  })

  it('clears the form after the trip is created', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit)

    await fill(wrapper, 'Tokyo', ['2026-12-01', '2026-12-05'])
    await submitForm(wrapper)

    expect(wrapper.find<HTMLInputElement>('input[name="name"]').element.value).toBe('')
    expect(wrapper.findComponent(VueDatePicker).props('modelValue')).toBeNull()
  })

  it('keeps the values when the trip is not created', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(false)
    const wrapper = mountForm(submit)

    await fill(wrapper, 'Tokyo', ['2026-12-01', '2026-12-05'])
    await submitForm(wrapper)

    expect(wrapper.find<HTMLInputElement>('input[name="name"]').element.value).toBe('Tokyo')
    expect(wrapper.findComponent(VueDatePicker).props('modelValue')).toEqual([
      '2026-12-01',
      '2026-12-05',
    ])
  })

  it('disables the button until the name and dates are filled in', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit)

    await fill(wrapper, 'Tokyo', null)

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('shows the maximum length before dates are selected', () => {
    const wrapper = mountForm(vi.fn<Submit>())

    expect(wrapper.find('[data-testid="dates-hint"]').text()).toBe('Up to 30 days')
  })

  it('shows the number of days after dates are selected', async () => {
    const wrapper = mountForm(vi.fn<Submit>())

    await fill(wrapper, 'Kyoto', ['2026-11-27', '2026-11-30'])

    expect(wrapper.find('[data-testid="dates-hint"]').text()).toBe('4 days')
  })

  it('limits the date picker to the maximum trip length', () => {
    const wrapper = mountForm(vi.fn<Submit>())

    expect(wrapper.findComponent(VueDatePicker).props('range')).toMatchObject({ maxRange: 29 })
  })
})

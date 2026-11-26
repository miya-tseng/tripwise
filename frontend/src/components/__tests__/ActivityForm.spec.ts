import { describe, expect, it, vi } from 'vitest'
import { DOMWrapper, flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import ActivityForm from '../ActivityForm.vue'
import type { Activity, SaveActivityInput } from '@/types/activity'
import type { ExchangeRates } from '@/types/currency'

const dates = ['2026-12-01', '2026-12-02']

const sensoji: Activity = {
  id: 'senso-ji-id',
  name: 'Senso-ji',
  date: '2026-12-02',
  startTime: '10:00:00',
  endTime: '11:30:00',
  cost: 2200.5,
  currency: 'TWD',
  costInTwd: 2200.5,
}

type Submit = (input: SaveActivityInput) => Promise<boolean>

const rates: ExchangeRates = {
  baseCurrency: 'TWD',
  available: true,
  updatedAt: '2026-01-01T00:00:00Z',
  currencies: ['JPY', 'TWD', 'USD'],
  unitsPerBaseCurrency: { TWD: 1, JPY: 4.8, USD: 0.032 },
}

function mountForm(
  submit: Submit = vi.fn<Submit>().mockResolvedValue(true),
  activity?: Activity,
  options: { rates?: ExchangeRates | null; defaultCurrency?: string } = {},
) {
  return mount(ActivityForm, { props: { dates, submit, activity, ...options } })
}

async function chooseCurrency(wrapper: ReturnType<typeof mountForm>, currency: string) {
  const select = wrapper.find('[data-testid="currency-select"]')
  await select.find('[data-testid="select-trigger"]').trigger('click')
  const option = select
    .findAll('[data-testid="select-option"]')
    .find((item) => item.find('.option-label').text() === currency)
  await option!.trigger('click')
}

function selectedCurrency(wrapper: ReturnType<typeof mountForm>): string {
  return (wrapper.find('input[name="currency"]').element as HTMLInputElement).value
}

async function pickTime(wrapper: VueWrapper, name: 'startTime' | 'endTime', time: string) {
  const picker = wrapper.find(`input[name="${name}"]`).element.parentElement!
  const scope = new DOMWrapper(picker)
  const [hour, minute] = time.split(':')
  await scope.find('[data-testid="time-trigger"]').trigger('click')
  const hourOption = scope
    .findAll('[data-testid="hour-option"]')
    .find((option) => option.text() === hour)
  await hourOption!.trigger('click')
  const minuteOption = scope
    .findAll('[data-testid="minute-option"]')
    .find((option) => option.text() === minute)
  await minuteOption!.trigger('click')
}

async function fill(
  wrapper: ReturnType<typeof mountForm>,
  values: { name?: string; date?: string; startTime?: string; endTime?: string },
) {
  if (values.name !== undefined) await wrapper.find('input[name="name"]').setValue(values.name)
  if (values.date !== undefined) await chooseDay(wrapper, values.date)
  if (values.startTime !== undefined) await pickTime(wrapper, 'startTime', values.startTime)
  if (values.endTime !== undefined) await pickTime(wrapper, 'endTime', values.endTime)
}

async function chooseDay(wrapper: ReturnType<typeof mountForm>, date: string) {
  const daySelect = wrapper.find('[data-testid="day-select"]')
  await daySelect.find('[data-testid="select-trigger"]').trigger('click')
  await daySelect.findAll('[data-testid="select-option"]')[dates.indexOf(date)]!.trigger('click')
}

function selectedDay(wrapper: ReturnType<typeof mountForm>): string {
  return (wrapper.find('input[name="date"]').element as HTMLInputElement).value
}

function submitButton(wrapper: ReturnType<typeof mountForm>) {
  return wrapper.find('button[type="submit"]')
}

describe('ActivityForm', () => {
  it('lists the trip days as options and selects the first day', () => {
    const wrapper = mountForm()

    const daySelect = wrapper.find('[data-testid="day-select"]')
    const options = daySelect
      .findAll('[data-testid="select-option"]')
      .map((option) => option.text())
    expect(options).toEqual(['Day 1Tue, Dec 1', 'Day 2Wed, Dec 2'])
    expect(selectedDay(wrapper)).toBe('2026-12-01')
    expect(daySelect.find('[data-testid="select-trigger"]').text()).toBe('Day 1Tue, Dec 1')
  })

  it('disables submit until name and both times are filled', async () => {
    const wrapper = mountForm()
    expect(submitButton(wrapper).attributes('disabled')).toBeDefined()

    await fill(wrapper, { name: 'Senso-ji', startTime: '10:00' })
    expect(submitButton(wrapper).attributes('disabled')).toBeDefined()

    await fill(wrapper, { endTime: '11:00' })
    expect(submitButton(wrapper).attributes('disabled')).toBeUndefined()
  })

  it.each([
    ['11:00', '10:00'],
    ['10:00', '10:00'],
  ])('shows an error and disables submit when %s – %s does not end later', async (start, end) => {
    const wrapper = mountForm()

    await fill(wrapper, { name: 'Senso-ji', startTime: start, endTime: end })

    expect(wrapper.find('[data-testid="time-error"]').text()).toBe(
      'End time must be later than start time.',
    )
    expect(submitButton(wrapper).attributes('disabled')).toBeDefined()
  })

  it('submits the values and clears the form after adding', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit)

    await fill(wrapper, {
      name: 'Senso-ji',
      date: '2026-12-02',
      startTime: '10:00',
      endTime: '11:00',
    })
    await wrapper.find('input[name="cost"]').setValue('2,200.5')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith({
      name: 'Senso-ji',
      date: '2026-12-02',
      startTime: '10:00',
      endTime: '11:00',
      cost: 2200.5,
      currency: 'TWD',
    })
    expect((wrapper.find('input[name="name"]').element as HTMLInputElement).value).toBe('')
    expect((wrapper.find('input[name="startTime"]').element as HTMLInputElement).value).toBe('')
    expect((wrapper.find('input[name="cost"]').element as HTMLInputElement).value).toBe('')
  })

  it('submits a zero cost when the cost is left empty', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit)

    await fill(wrapper, { name: 'Senso-ji', startTime: '10:00', endTime: '11:00' })
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith(expect.objectContaining({ cost: 0 }))
  })

  it.each(['-5', '12.345', 'abc'])(
    'shows an error and disables submit for the cost %j',
    async (cost) => {
      const wrapper = mountForm()

      await fill(wrapper, { name: 'Senso-ji', startTime: '10:00', endTime: '11:00' })
      await wrapper.find('input[name="cost"]').setValue(cost)

      expect(wrapper.find('[data-testid="cost-error"]').text()).toBe(
        'Cost must be a positive amount with up to 2 decimal places.',
      )
      expect(wrapper.find('input[name="cost"]').attributes('aria-invalid')).toBe('true')
      expect(submitButton(wrapper).attributes('disabled')).toBeDefined()
    },
  )

  it('keeps the values when adding fails', async () => {
    const wrapper = mountForm(vi.fn<Submit>().mockResolvedValue(false))

    await fill(wrapper, { name: 'Senso-ji', startTime: '10:00', endTime: '11:00' })
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect((wrapper.find('input[name="name"]').element as HTMLInputElement).value).toBe('Senso-ji')
  })

  it('fills in the activity when editing', () => {
    const wrapper = mountForm(undefined, sensoji)

    expect((wrapper.find('input[name="name"]').element as HTMLInputElement).value).toBe('Senso-ji')
    expect(selectedDay(wrapper)).toBe('2026-12-02')
    expect((wrapper.find('input[name="startTime"]').element as HTMLInputElement).value).toBe(
      '10:00',
    )
    expect((wrapper.find('input[name="endTime"]').element as HTMLInputElement).value).toBe('11:30')
    expect((wrapper.find('input[name="cost"]').element as HTMLInputElement).value).toBe('2200.50')
    expect(submitButton(wrapper).text()).toBe('Save')
  })

  it('leaves the cost empty when editing a free activity', () => {
    const wrapper = mountForm(undefined, { ...sensoji, cost: 0 })

    expect((wrapper.find('input[name="cost"]').element as HTMLInputElement).value).toBe('')
  })

  it('emits cancel when editing is cancelled', async () => {
    const wrapper = mountForm(undefined, sensoji)

    await wrapper.find('[data-testid="cancel-edit"]').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('does not show a cancel button when adding', () => {
    const wrapper = mountForm()

    expect(wrapper.find('[data-testid="cancel-edit"]').exists()).toBe(false)
    expect(submitButton(wrapper).text()).toBe('Add')
  })

  it('defaults the currency to TWD', () => {
    expect(selectedCurrency(mountForm())).toBe('TWD')
  })

  it('uses the given default currency for new activities', () => {
    const wrapper = mountForm(undefined, undefined, { defaultCurrency: 'JPY' })

    expect(selectedCurrency(wrapper)).toBe('JPY')
  })

  it('uses the currency of the activity when editing', () => {
    const wrapper = mountForm(
      undefined,
      { ...sensoji, currency: 'USD' },
      { defaultCurrency: 'JPY' },
    )

    expect(selectedCurrency(wrapper)).toBe('USD')
  })

  it('offers only common currencies that have exchange rates', async () => {
    const wrapper = mountForm(undefined, undefined, { rates })

    await wrapper
      .find('[data-testid="currency-select"] [data-testid="select-trigger"]')
      .trigger('click')

    const codes = wrapper
      .findAll('[data-testid="currency-select"] .option-label')
      .map((option) => option.text())
    expect(codes).toEqual(['TWD', 'JPY', 'USD'])
    expect(wrapper.find('[data-testid="currency-select"]').text()).toContain('Japanese Yen')
  })

  it('submits the chosen currency', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountForm(submit, undefined, { rates })

    await fill(wrapper, { name: 'Shibuya Sky', startTime: '17:00', endTime: '18:00' })
    await wrapper.find('input[name="cost"]').setValue('2200')
    await chooseCurrency(wrapper, 'JPY')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith(expect.objectContaining({ cost: 2200, currency: 'JPY' }))
  })

  it('keeps the chosen currency after adding', async () => {
    const wrapper = mountForm(undefined, undefined, { rates })

    await fill(wrapper, { name: 'Shibuya Sky', startTime: '17:00', endTime: '18:00' })
    await chooseCurrency(wrapper, 'JPY')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(selectedCurrency(wrapper)).toBe('JPY')
  })

  it('previews the cost in TWD for a foreign currency', async () => {
    const wrapper = mountForm(undefined, undefined, { rates })

    await wrapper.find('input[name="cost"]').setValue('2200')
    await chooseCurrency(wrapper, 'JPY')

    expect(wrapper.find('[data-testid="cost-preview"]').text()).toBe('≈ NT$458.33')
  })

  it('does not preview TWD or empty costs', async () => {
    const wrapper = mountForm(undefined, undefined, { rates })

    await wrapper.find('input[name="cost"]').setValue('2200')
    expect(wrapper.find('[data-testid="cost-preview"]').exists()).toBe(false)

    await chooseCurrency(wrapper, 'JPY')
    await wrapper.find('input[name="cost"]').setValue('')
    expect(wrapper.find('[data-testid="cost-preview"]').exists()).toBe(false)
  })

  it('says when the TWD amount is unavailable', async () => {
    const wrapper = mountForm(undefined, undefined, { rates: { ...rates, available: false } })

    await wrapper.find('input[name="cost"]').setValue('2200')
    await chooseCurrency(wrapper, 'JPY')

    expect(wrapper.find('[data-testid="cost-preview"]').text()).toBe('TWD amount unavailable')
  })
})

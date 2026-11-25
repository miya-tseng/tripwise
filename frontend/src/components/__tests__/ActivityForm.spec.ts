import { describe, expect, it, vi } from 'vitest'
import { DOMWrapper, flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import ActivityForm from '../ActivityForm.vue'
import type { Activity, SaveActivityInput } from '@/types/activity'

const dates = ['2026-12-01', '2026-12-02']

const sensoji: Activity = {
  id: 'senso-ji-id',
  name: 'Senso-ji',
  date: '2026-12-02',
  startTime: '10:00:00',
  endTime: '11:30:00',
  cost: 2200.5,
}

type Submit = (input: SaveActivityInput) => Promise<boolean>

function mountForm(submit: Submit = vi.fn<Submit>().mockResolvedValue(true), activity?: Activity) {
  return mount(ActivityForm, { props: { dates, submit, activity } })
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
  await wrapper.find('[data-testid="select-trigger"]').trigger('click')
  await wrapper.findAll('[data-testid="select-option"]')[dates.indexOf(date)]!.trigger('click')
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

    const options = wrapper.findAll('[data-testid="select-option"]').map((option) => option.text())
    expect(options).toEqual(['Day 1Tue, Dec 1', 'Day 2Wed, Dec 2'])
    expect(selectedDay(wrapper)).toBe('2026-12-01')
    expect(wrapper.find('[data-testid="select-trigger"]').text()).toBe('Day 1Tue, Dec 1')
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
})

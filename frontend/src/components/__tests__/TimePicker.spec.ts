import { afterEach, describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import TimePicker from '../TimePicker.vue'

let wrapper: VueWrapper | null = null

function mountPicker(modelValue = '') {
  wrapper = mount(TimePicker, {
    props: {
      modelValue,
      name: 'startTime',
      label: 'Start',
      'onUpdate:modelValue': (value: string) => wrapper!.setProps({ modelValue: value }),
    },
    attachTo: document.body,
  })
  return wrapper
}

function trigger(picker: VueWrapper) {
  return picker.find('[data-testid="time-trigger"]')
}

function isOpen(picker: VueWrapper): boolean {
  return trigger(picker).attributes('aria-expanded') === 'true'
}

function option(picker: VueWrapper, kind: 'hour' | 'minute', text: string) {
  return picker.findAll(`[data-testid="${kind}-option"]`).find((item) => item.text() === text)!
}

function emitted(picker: VueWrapper): string[] {
  return (picker.emitted('update:modelValue') ?? []).map(([value]) => value as string)
}

describe('TimePicker', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('shows a placeholder when no time is set', () => {
    const picker = mountPicker()

    expect(trigger(picker).text()).toBe('--:--')
    expect((picker.find('input[name="startTime"]').element as HTMLInputElement).value).toBe('')
  })

  it('shows the time and keeps it in a hidden input', () => {
    const picker = mountPicker('10:30')

    expect(picker.find('[data-testid="time-value"]').text()).toBe('10:30')
    expect((picker.find('input[name="startTime"]').element as HTMLInputElement).value).toBe('10:30')
  })

  it('lists every hour and minutes in five minute steps', async () => {
    const picker = mountPicker()

    await trigger(picker).trigger('click')

    expect(picker.findAll('[data-testid="hour-option"]')).toHaveLength(24)
    expect(picker.findAll('[data-testid="minute-option"]').map((item) => item.text())).toEqual([
      '00',
      '05',
      '10',
      '15',
      '20',
      '25',
      '30',
      '35',
      '40',
      '45',
      '50',
      '55',
    ])
  })

  it('keeps an off-step minute of the current value in the list', async () => {
    const picker = mountPicker('10:07')

    await trigger(picker).trigger('click')

    expect(option(picker, 'minute', '07').attributes('aria-pressed')).toBe('true')
  })

  it('marks the selected hour and minute', async () => {
    const picker = mountPicker('10:30')

    await trigger(picker).trigger('click')

    expect(option(picker, 'hour', '10').attributes('aria-pressed')).toBe('true')
    expect(option(picker, 'minute', '30').attributes('aria-pressed')).toBe('true')
    expect(option(picker, 'hour', '11').attributes('aria-pressed')).toBe('false')
  })

  it('picks the hour first and closes after the minute', async () => {
    const picker = mountPicker()

    await trigger(picker).trigger('click')
    await option(picker, 'hour', '14').trigger('click')
    expect(isOpen(picker)).toBe(true)

    await option(picker, 'minute', '45').trigger('click')

    expect(emitted(picker)).toEqual(['14:00', '14:45'])
    expect(isOpen(picker)).toBe(false)
  })

  it('keeps the minute when only the hour changes', async () => {
    const picker = mountPicker('10:30')

    await trigger(picker).trigger('click')
    await option(picker, 'hour', '08').trigger('click')

    expect(emitted(picker)).toEqual(['08:30'])
  })

  it('opens with the keyboard and closes with escape', async () => {
    const picker = mountPicker()

    await trigger(picker).trigger('keydown', { key: 'ArrowDown' })
    expect(isOpen(picker)).toBe(true)

    await picker.find('[data-testid="time-panel"]').trigger('keydown', { key: 'Escape' })
    expect(isOpen(picker)).toBe(false)
    expect(emitted(picker)).toEqual([])
  })

  it('closes when clicking outside', async () => {
    const picker = mountPicker()

    await trigger(picker).trigger('click')
    document.body.dispatchEvent(new Event('pointerdown', { bubbles: true }))
    await picker.vm.$nextTick()

    expect(isOpen(picker)).toBe(false)
  })

  it('describes the value for screen readers', () => {
    expect(trigger(mountPicker('10:30')).attributes('aria-label')).toBe('Start: 10:30')
  })
})

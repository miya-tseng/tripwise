import { afterEach, describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import SelectMenu from '../SelectMenu.vue'

const options = [
  { value: 'a', label: 'Day 1', hint: 'Tue, Dec 1' },
  { value: 'b', label: 'Day 2', hint: 'Wed, Dec 2' },
  { value: 'c', label: 'Day 3', hint: 'Thu, Dec 3' },
]

let wrapper: VueWrapper | null = null

function mountSelect(modelValue = 'a') {
  wrapper = mount(SelectMenu, {
    props: {
      modelValue,
      options,
      name: 'date',
      'onUpdate:modelValue': (value: string) => wrapper!.setProps({ modelValue: value }),
    },
    attachTo: document.body,
  })
  return wrapper
}

function trigger(select: VueWrapper) {
  return select.find('[data-testid="select-trigger"]')
}

function menuIsOpen(select: VueWrapper): boolean {
  return trigger(select).attributes('aria-expanded') === 'true'
}

function activeLabel(select: VueWrapper): string | undefined {
  return select.find('[data-testid="select-option"].active .option-label').text()
}

describe('SelectMenu', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('shows the selected option and keeps its value in a hidden input', () => {
    const select = mountSelect('b')

    expect(trigger(select).text()).toBe('Day 2Wed, Dec 2')
    expect((select.find('input[name="date"]').element as HTMLInputElement).value).toBe('b')
    expect(menuIsOpen(select)).toBe(false)
  })

  it('opens on click and marks the selected option', async () => {
    const select = mountSelect('b')

    await trigger(select).trigger('click')

    expect(menuIsOpen(select)).toBe(true)
    const selected = select
      .findAll('[role="option"]')
      .map((option) => option.attributes('aria-selected'))
    expect(selected).toEqual(['false', 'true', 'false'])
    expect(activeLabel(select)).toBe('Day 2')
  })

  it('selects an option on click and closes', async () => {
    const select = mountSelect()

    await trigger(select).trigger('click')
    await select.findAll('[data-testid="select-option"]')[2]!.trigger('click')

    expect(select.emitted('update:modelValue')).toEqual([['c']])
    expect(menuIsOpen(select)).toBe(false)
    expect(trigger(select).text()).toBe('Day 3Thu, Dec 3')
  })

  it('opens with arrow down and selects with enter', async () => {
    const select = mountSelect()

    await trigger(select).trigger('keydown', { key: 'ArrowDown' })
    expect(menuIsOpen(select)).toBe(true)

    await trigger(select).trigger('keydown', { key: 'ArrowDown' })
    expect(activeLabel(select)).toBe('Day 2')

    await trigger(select).trigger('keydown', { key: 'Enter' })
    expect(select.emitted('update:modelValue')).toEqual([['b']])
    expect(menuIsOpen(select)).toBe(false)
  })

  it('wraps around when moving past the ends', async () => {
    const select = mountSelect()

    await trigger(select).trigger('keydown', { key: 'ArrowDown' })
    await trigger(select).trigger('keydown', { key: 'ArrowUp' })
    expect(activeLabel(select)).toBe('Day 3')

    await trigger(select).trigger('keydown', { key: 'Home' })
    expect(activeLabel(select)).toBe('Day 1')
  })

  it('closes with escape without changing the value', async () => {
    const select = mountSelect()

    await trigger(select).trigger('click')
    await trigger(select).trigger('keydown', { key: 'Escape' })

    expect(menuIsOpen(select)).toBe(false)
    expect(select.emitted('update:modelValue')).toBeUndefined()
  })

  it('closes when clicking outside', async () => {
    const select = mountSelect()

    await trigger(select).trigger('click')
    document.body.dispatchEvent(new Event('pointerdown', { bubbles: true }))
    await select.vm.$nextTick()

    expect(menuIsOpen(select)).toBe(false)
  })

  it('points aria-activedescendant at the active option', async () => {
    const select = mountSelect()

    await trigger(select).trigger('click')

    const activeId = trigger(select).attributes('aria-activedescendant')
    expect(select.find(`#${CSS.escape(activeId!)}`).text()).toContain('Day 1')
  })
})

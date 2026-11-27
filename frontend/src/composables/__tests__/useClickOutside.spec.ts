import { describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, ref } from 'vue'
import { mount } from '@vue/test-utils'
import { useClickOutside } from '../useClickOutside'

function mountProbe(onOutside: () => void) {
  const open = ref(false)
  const wrapper = mount(
    defineComponent({
      setup() {
        const root = ref<HTMLElement | null>(null)
        useClickOutside(root, open, onOutside)
        return () => h('div', { ref: root, id: 'inside' }, [h('button', { id: 'child' })])
      },
    }),
    { attachTo: document.body },
  )
  return { wrapper, open }
}

function pointerDownOn(element: Element) {
  element.dispatchEvent(new Event('pointerdown', { bubbles: true }))
}

describe('useClickOutside', () => {
  it('calls back only for presses outside while active', async () => {
    const onOutside = vi.fn<() => void>()
    const { wrapper, open } = mountProbe(onOutside)

    pointerDownOn(document.body)
    expect(onOutside).not.toHaveBeenCalled()

    open.value = true
    await nextTick()
    pointerDownOn(wrapper.find('#child').element)
    expect(onOutside).not.toHaveBeenCalled()

    pointerDownOn(document.body)
    expect(onOutside).toHaveBeenCalledTimes(1)

    open.value = false
    await nextTick()
    pointerDownOn(document.body)
    expect(onOutside).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('stops listening when unmounted', async () => {
    const onOutside = vi.fn<() => void>()
    const { wrapper, open } = mountProbe(onOutside)
    open.value = true
    await nextTick()

    wrapper.unmount()
    pointerDownOn(document.body)

    expect(onOutside).not.toHaveBeenCalled()
  })
})

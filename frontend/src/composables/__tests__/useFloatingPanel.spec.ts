import { afterEach, describe, expect, it } from 'vitest'
import { defineComponent, h, nextTick, ref } from 'vue'
import { mount } from '@vue/test-utils'
import { useFloatingPanel, type FloatingPanelOptions } from '../useFloatingPanel'

function rect(top: number, left: number, width = 200, height = 40): DOMRect {
  return { top, left, width, height, bottom: top + height, right: left + width } as DOMRect
}

function mountFloating(
  anchorRect: DOMRect,
  panelHeight: number,
  options: FloatingPanelOptions = {},
) {
  const open = ref(false)
  let result: ReturnType<typeof useFloatingPanel> | undefined
  const Host = defineComponent({
    setup() {
      const anchor = ref<HTMLElement | null>(null)
      const panel = ref<HTMLElement | null>(null)
      result = useFloatingPanel(anchor, panel, open, options)
      return () =>
        h('div', [h('button', { ref: anchor }), open.value ? h('div', { ref: panel }) : null])
    },
  })
  const wrapper = mount(Host, { attachTo: document.body })
  wrapper.find('button').element.getBoundingClientRect = () => anchorRect
  Object.defineProperty(HTMLElement.prototype, 'offsetHeight', {
    configurable: true,
    get: () => panelHeight,
  })
  return { wrapper, open, floating: () => result! }
}

async function settle() {
  await nextTick()
  await nextTick()
}

describe('useFloatingPanel', () => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('places the panel below the anchor with a fixed position', async () => {
    window.innerHeight = 800
    const { open, floating } = mountFloating(rect(100, 50), 200)

    open.value = true
    await settle()

    expect(floating().placement.value).toBe('below')
    expect(floating().style.value).toMatchObject({
      position: 'fixed',
      top: '146px',
      left: '50px',
      minWidth: '200px',
    })
  })

  it('flips above the anchor when there is no room below', async () => {
    window.innerHeight = 800
    const { open, floating } = mountFloating(rect(650, 50), 300)

    open.value = true
    await settle()

    expect(floating().placement.value).toBe('above')
    expect(floating().style.value).toMatchObject({ bottom: '156px' })
    expect(floating().style.value.top).toBeUndefined()
  })

  it('aligns to the right edge when asked', async () => {
    window.innerHeight = 800
    window.innerWidth = 1000
    const { open, floating } = mountFloating(rect(100, 600), 100, { align: 'end' })

    open.value = true
    await settle()

    expect(floating().style.value).toMatchObject({ right: '200px' })
    expect(floating().style.value.left).toBeUndefined()
  })

  it('limits the height to the available space', async () => {
    window.innerHeight = 800
    const { open, floating } = mountFloating(rect(100, 50), 100)

    open.value = true
    await settle()

    expect(floating().style.value.maxHeight).toBe('646px')
  })

  it('follows the anchor when the page scrolls', async () => {
    window.innerHeight = 800
    const { wrapper, open, floating } = mountFloating(rect(100, 50), 100)
    open.value = true
    await settle()

    wrapper.find('button').element.getBoundingClientRect = () => rect(60, 50)
    window.dispatchEvent(new Event('scroll'))

    expect(floating().style.value.top).toBe('106px')
  })
})

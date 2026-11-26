import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { mount } from '@vue/test-utils'
import { useMediaQuery } from '../useMediaQuery'

function fakeMatchMedia(initial: boolean) {
  let listener: (() => void) | null = null
  const list = {
    matches: initial,
    addEventListener: (_: string, callback: () => void) => {
      listener = callback
    },
    removeEventListener: vi.fn<() => void>(),
  }
  vi.stubGlobal('matchMedia', () => list)
  return {
    list,
    change(matches: boolean) {
      list.matches = matches
      listener?.()
    },
  }
}

function mountProbe() {
  return mount(
    defineComponent({
      setup() {
        const phone = useMediaQuery('(max-width: 720px)')
        return () => h('span', phone.value ? 'phone' : 'wide')
      },
    }),
  )
}

describe('useMediaQuery', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('follows the media query', async () => {
    const media = fakeMatchMedia(true)
    const wrapper = mountProbe()

    expect(wrapper.text()).toBe('phone')

    media.change(false)
    await nextTick()
    expect(wrapper.text()).toBe('wide')

    wrapper.unmount()
    expect(media.list.removeEventListener).toHaveBeenCalled()
  })
})

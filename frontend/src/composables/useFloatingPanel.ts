import { nextTick, onBeforeUnmount, ref, watch, type Ref } from 'vue'

export interface FloatingPanelOptions {
  align?: 'start' | 'end'
  gap?: number
  matchWidth?: boolean
}

export type FloatingPlacement = 'below' | 'above'

const VIEWPORT_MARGIN = 8

export function useFloatingPanel(
  anchor: Ref<HTMLElement | null>,
  panel: Ref<HTMLElement | null>,
  open: Ref<boolean>,
  options: FloatingPanelOptions = {},
) {
  const { align = 'start', gap = 6, matchWidth = true } = options
  const style = ref<Record<string, string>>({})
  const placement = ref<FloatingPlacement>('below')

  function update() {
    if (!anchor.value) {
      return
    }

    const rect = anchor.value.getBoundingClientRect()
    const panelHeight = panel.value?.offsetHeight ?? 0
    const viewportHeight = window.innerHeight
    const viewportWidth = window.innerWidth
    const spaceBelow = viewportHeight - rect.bottom - gap - VIEWPORT_MARGIN
    const spaceAbove = rect.top - gap - VIEWPORT_MARGIN
    const flip = panelHeight > spaceBelow && spaceAbove > spaceBelow

    placement.value = flip ? 'above' : 'below'

    const next: Record<string, string> = {
      position: 'fixed',
      maxHeight: `${Math.max(flip ? spaceAbove : spaceBelow, 120)}px`,
    }

    if (flip) {
      next.bottom = `${viewportHeight - rect.top + gap}px`
    } else {
      next.top = `${rect.bottom + gap}px`
    }

    if (align === 'end') {
      next.right = `${Math.max(viewportWidth - rect.right, VIEWPORT_MARGIN)}px`
    } else {
      next.left = `${Math.max(rect.left, VIEWPORT_MARGIN)}px`
    }

    if (matchWidth) {
      next.minWidth = `${rect.width}px`
    }

    style.value = next
  }

  function listen() {
    window.addEventListener('scroll', update, true)
    window.addEventListener('resize', update)
  }

  function unlisten() {
    window.removeEventListener('scroll', update, true)
    window.removeEventListener('resize', update)
  }

  watch(open, async (isOpen) => {
    if (isOpen) {
      update()
      listen()
      await nextTick()
      update()
    } else {
      unlisten()
    }
  })

  onBeforeUnmount(unlisten)

  return { style, placement, update }
}

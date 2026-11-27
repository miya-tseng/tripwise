import { onBeforeUnmount, ref, watch, type Ref } from 'vue'

export function useElementHeight(target: Ref<HTMLElement | null | undefined>): Ref<number> {
  const height = ref(0)
  let observer: ResizeObserver | undefined

  watch(
    target,
    (element) => {
      observer?.disconnect()

      if (!element || typeof ResizeObserver === 'undefined') {
        return
      }

      observer = new ResizeObserver(() => {
        height.value = element.offsetHeight
      })
      observer.observe(element)
    },
    { immediate: true },
  )

  onBeforeUnmount(() => observer?.disconnect())

  return height
}

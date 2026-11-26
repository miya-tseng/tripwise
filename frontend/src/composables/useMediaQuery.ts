import { onBeforeUnmount, ref, type Ref } from 'vue'

export function useMediaQuery(query: string): Ref<boolean> {
  const matches = ref(false)

  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return matches
  }

  const list = window.matchMedia(query)
  const update = () => {
    matches.value = list.matches
  }

  update()
  list.addEventListener?.('change', update)
  onBeforeUnmount(() => list.removeEventListener?.('change', update))

  return matches
}

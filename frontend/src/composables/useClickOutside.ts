import { onBeforeUnmount, watch, type Ref } from 'vue'

export function useClickOutside(
  target: Ref<HTMLElement | null>,
  active: Ref<boolean>,
  onOutside: () => void,
): void {
  function onPointerDown(event: PointerEvent) {
    if (target.value && !target.value.contains(event.target as Node)) {
      onOutside()
    }
  }

  function stopListening() {
    document.removeEventListener('pointerdown', onPointerDown)
  }

  watch(active, (isActive) => {
    if (isActive) {
      document.addEventListener('pointerdown', onPointerDown)
    } else {
      stopListening()
    }
  })

  onBeforeUnmount(stopListening)
}

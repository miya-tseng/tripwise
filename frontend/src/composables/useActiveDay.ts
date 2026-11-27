import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch, type Ref } from 'vue'

const DAY_SELECTOR = '[data-testid="schedule-day"]'
const SWIPE_DISTANCE = 60
const JUMP_SETTLE_MS = 900

interface ActiveDayOptions {
  dates: Ref<string[]>
  paged: Ref<boolean>
  headerOffset: Ref<number>
  ready: Ref<boolean>
  linkedDay: Ref<string | undefined>
  today: Ref<string>
}

function dayElements(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>(DAY_SELECTOR))
}

export function useActiveDay(options: ActiveDayOptions) {
  const { dates, paged, headerOffset, ready, linkedDay, today } = options
  const activeDayIndex = ref(0)
  const currentDayDate = computed(() => dates.value[activeDayIndex.value] ?? dates.value[0])

  let scrollFrame = 0
  let jumpLockUntil = 0
  let touchStart: { x: number; y: number } | null = null

  function isVisible(element: HTMLElement | undefined): boolean {
    if (!element) {
      return false
    }

    const box = element.getBoundingClientRect()
    return box.bottom > headerOffset.value && box.top < window.innerHeight
  }

  function followScroll() {
    scrollFrame = 0
    const days = dayElements()

    if (paged.value || days.length === 0 || Date.now() < jumpLockUntil) {
      return
    }

    const atBottom =
      window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2

    if (atBottom && isVisible(days[activeDayIndex.value])) {
      return
    }

    const line = headerOffset.value + 24
    const passed = days.filter((element) => element.getBoundingClientRect().top <= line).length
    activeDayIndex.value = Math.max(0, passed - 1)
  }

  function onScroll() {
    if (!scrollFrame) {
      scrollFrame = requestAnimationFrame(followScroll)
    }
  }

  function jumpToDay(index: number) {
    activeDayIndex.value = index

    if (paged.value) {
      window.scrollTo?.({ top: 0, behavior: 'smooth' })
      return
    }

    jumpLockUntil = Date.now() + JUMP_SETTLE_MS
    dayElements()[index]?.scrollIntoView?.({ block: 'start', behavior: 'smooth' })
  }

  function showDay(step: number) {
    const next = activeDayIndex.value + step

    if (next >= 0 && next < dates.value.length) {
      jumpToDay(next)
    }
  }

  function onTouchStart(event: TouchEvent) {
    const touch = event.touches[0]
    touchStart = touch ? { x: touch.clientX, y: touch.clientY } : null
  }

  function onTouchEnd(event: TouchEvent) {
    const touch = event.changedTouches[0]

    if (!paged.value || !touchStart || !touch) {
      return
    }

    const dx = touch.clientX - touchStart.x
    const dy = touch.clientY - touchStart.y
    touchStart = null

    if (Math.abs(dx) > SWIPE_DISTANCE && Math.abs(dx) > Math.abs(dy) * 1.5) {
      showDay(dx < 0 ? 1 : -1)
    }
  }

  async function openStartingDay() {
    const dayNumber = linkedDay.value
      ? Number(linkedDay.value)
      : dates.value.indexOf(today.value) + 1

    if (!Number.isInteger(dayNumber) || dayNumber < 1) {
      return
    }

    if (paged.value) {
      activeDayIndex.value = Math.min(dayNumber, dates.value.length) - 1
      return
    }

    const firstDayIsAlreadyInView = !linkedDay.value && dayNumber === 1

    if (firstDayIsAlreadyInView) {
      return
    }

    await nextTick()
    dayElements()[dayNumber - 1]?.scrollIntoView?.({ block: 'start', behavior: 'smooth' })
  }

  watch(
    ready,
    (isReady) => {
      if (isReady) {
        void openStartingDay()
      }
    },
    { immediate: true },
  )

  onMounted(() => window.addEventListener('scroll', onScroll, { passive: true }))

  onBeforeUnmount(() => {
    window.removeEventListener('scroll', onScroll)
    cancelAnimationFrame(scrollFrame)
  })

  return { activeDayIndex, currentDayDate, jumpToDay, showDay, onTouchStart, onTouchEnd }
}

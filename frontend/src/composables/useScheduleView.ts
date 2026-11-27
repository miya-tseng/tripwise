import { inject, ref, watch, type Ref } from 'vue'
import { routerKey } from 'vue-router'
import { parseView, type ScheduleView } from '@/utils/activityTable'
import { lastUsedView, rememberView } from '@/utils/preferences'

export const VIEW_OPTIONS: { value: ScheduleView; label: string }[] = [
  { value: 'timeline', label: 'Timeline' },
  { value: 'board', label: 'Board' },
  { value: 'table', label: 'Table' },
]

export function useScheduleView(tripId: Ref<string>, viewInUrl: Ref<string | undefined>) {
  const router = inject(routerKey, null)

  function initialView(id: string): ScheduleView {
    return parseView(viewInUrl.value) ?? parseView(lastUsedView(id)) ?? 'timeline'
  }

  const scheduleView = ref<ScheduleView>(initialView(tripId.value))

  function setView(view: ScheduleView) {
    scheduleView.value = view
    rememberView(tripId.value, view)

    const query = { ...router?.currentRoute.value.query }
    delete query.day

    if (view === 'timeline') {
      delete query.view
    } else {
      query.view = view
    }

    router?.replace({ query })
  }

  watch(viewInUrl, (view) => {
    const parsed = parseView(view)

    if (parsed) {
      scheduleView.value = parsed
    }
  })

  watch(tripId, (id) => {
    scheduleView.value = initialView(id)
  })

  return { scheduleView, setView }
}

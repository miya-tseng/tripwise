import { computed, ref, type Ref } from 'vue'
import type { SelectOption } from '@/components/SelectMenu.vue'
import type { ActivityCategory, BookingStatus, ScheduleDay } from '@/types/activity'
import { BOOKING_STATUSES, CATEGORIES } from '@/utils/activityDetails'
import { filterDays } from '@/utils/activityTable'

export function useActivityFilters(days: Ref<ScheduleDay[]>) {
  const category = ref<ActivityCategory | null>(null)
  const bookingStatus = ref<BookingStatus | null>(null)

  const activities = computed(() => days.value.flatMap((day) => day.activities))

  const categoriesInTrip = computed(() => {
    const used = new Set(activities.value.map((activity) => activity.category))
    return CATEGORIES.filter((item) => used.has(item.value))
  })

  const bookingOptions = computed<SelectOption[]>(() => {
    const used = new Set(activities.value.map((activity) => activity.bookingStatus))
    return [
      { value: '', label: 'All bookings' },
      ...BOOKING_STATUSES.filter((status) => used.has(status.value)).map((status) => ({
        value: status.value,
        label: status.label,
      })),
    ]
  })

  const visibleDays = computed(() =>
    filterDays(days.value, { category: category.value, bookingStatus: bookingStatus.value }),
  )

  const bookingValue = computed({
    get: () => bookingStatus.value ?? '',
    set: (value: string) => {
      bookingStatus.value = value === '' ? null : (value as BookingStatus)
    },
  })

  const categoryValue = computed({
    get: () => category.value as string | null,
    set: (value: string | null) => {
      category.value = value as ActivityCategory | null
    },
  })

  return { visibleDays, categoriesInTrip, bookingOptions, bookingValue, categoryValue }
}

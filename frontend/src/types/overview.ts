import type { ActivityCategory } from './activity'
import type { CurrencyAmount } from './currency'

export type AttentionKind = 'ActivityToBook' | 'StayToBook' | 'NightWithoutStay' | 'EmptyDay'

export interface CurrencySpend {
  currency: string
  amount: number
  amountInTwd: number | null
}

export interface CategorySpend {
  category: ActivityCategory
  activityCount: number
  amountInTwd: number | null
}

export interface DaySpend {
  date: string
  dayNumber: number
  activityCount: number
  activitiesInTwd: number | null
  stayInTwd: number | null
  totalInTwd: number | null
}

export interface Expense {
  kind: 'Activity' | 'Stay'
  id: string
  name: string
  date: string
  cost: CurrencyAmount
  costInTwd: number | null
}

export interface AttentionItem {
  kind: AttentionKind
  date: string
  itemId: string | null
  name: string | null
}

export interface TripOverview {
  tripId: string
  name: string
  startDate: string
  endDate: string
  days: number
  nights: number
  nightsWithStay: number
  activityCount: number
  placeCount: number
  ratesAvailable: boolean
  totalInTwd: number | null
  activitiesInTwd: number | null
  staysInTwd: number | null
  averagePerDayInTwd: number | null
  spendByCurrency: CurrencySpend[]
  spendByCategory: CategorySpend[]
  spendByDay: DaySpend[]
  booking: { toBook: number; booked: number; paid: number }
  topExpenses: Expense[]
  attention: AttentionItem[]
}

import type { Place } from './place'

export interface Suggestion {
  place: Place
  reason: string
  suggestedMinutes: number
}

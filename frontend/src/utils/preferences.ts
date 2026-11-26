import { HOME_CURRENCY } from '@/types/currency'

const currencyKey = (tripId: string) => `tripwise.currency.${tripId}`

export function lastUsedCurrency(tripId: string): string {
  try {
    return localStorage.getItem(currencyKey(tripId)) ?? HOME_CURRENCY
  } catch {
    return HOME_CURRENCY
  }
}

export function rememberCurrency(tripId: string, currency: string): void {
  try {
    localStorage.setItem(currencyKey(tripId), currency)
  } catch {
    return
  }
}

const viewKey = (tripId: string) => `tripwise.view.${tripId}`

export function lastUsedView(tripId: string): string | null {
  try {
    return localStorage.getItem(viewKey(tripId))
  } catch {
    return null
  }
}

export function rememberView(tripId: string, view: string): void {
  try {
    localStorage.setItem(viewKey(tripId), view)
  } catch {
    return
  }
}

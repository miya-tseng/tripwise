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

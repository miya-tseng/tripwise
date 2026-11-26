export const HOME_CURRENCY = 'TWD'

export const COMMON_CURRENCIES = [
  'TWD',
  'JPY',
  'USD',
  'EUR',
  'KRW',
  'THB',
  'HKD',
  'CNY',
  'SGD',
  'GBP',
  'AUD',
  'VND',
  'MYR',
  'PHP',
  'IDR',
  'CAD',
  'CHF',
  'NZD',
] as const

export interface ExchangeRates {
  baseCurrency: string
  available: boolean
  updatedAt: string | null
  currencies: string[]
  unitsPerBaseCurrency: Record<string, number>
}

export interface CurrencyAmount {
  amount: number
  currency: string
}

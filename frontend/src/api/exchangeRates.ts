import type { ExchangeRates } from '@/types/currency'
import { request } from './http'

export async function fetchExchangeRates(): Promise<ExchangeRates> {
  const response = await request('/api/exchange-rates')
  return response.json()
}

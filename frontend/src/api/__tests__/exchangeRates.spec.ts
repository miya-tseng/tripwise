import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fetchExchangeRates } from '../exchangeRates'

describe('exchange rates api', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('gets the latest exchange rates', async () => {
    const rates = {
      baseCurrency: 'TWD',
      available: true,
      updatedAt: '2026-01-01T00:00:00Z',
      currencies: ['JPY', 'TWD'],
      unitsPerBaseCurrency: { TWD: 1, JPY: 4.8 },
    }
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(rates), { headers: { 'Content-Type': 'application/json' } }),
    )

    const result = await fetchExchangeRates()

    expect(fetchMock).toHaveBeenCalledWith('/api/exchange-rates')
    expect(result).toEqual(rates)
  })
})

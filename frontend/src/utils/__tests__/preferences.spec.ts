import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryStorage } from '@/test/memoryStorage'
import { lastUsedCurrency, rememberCurrency } from '../preferences'

describe('currency preferences', () => {
  beforeEach(() => {
    vi.stubGlobal('localStorage', createMemoryStorage())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('defaults to TWD', () => {
    expect(lastUsedCurrency('trip-a')).toBe('TWD')
  })

  it('remembers the last currency per trip', () => {
    rememberCurrency('trip-a', 'JPY')
    rememberCurrency('trip-b', 'USD')

    expect(lastUsedCurrency('trip-a')).toBe('JPY')
    expect(lastUsedCurrency('trip-b')).toBe('USD')
  })
})

describe('currency preferences without storage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('falls back to TWD and ignores saving when storage throws', () => {
    vi.stubGlobal('localStorage', {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      },
    })

    expect(() => rememberCurrency('trip-a', 'JPY')).not.toThrow()
    expect(lastUsedCurrency('trip-a')).toBe('TWD')
  })
})

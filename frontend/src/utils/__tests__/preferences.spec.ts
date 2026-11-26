import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryStorage } from '@/test/memoryStorage'
import { lastUsedCurrency, lastUsedView, rememberCurrency, rememberView } from '../preferences'

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

describe('view preferences', () => {
  beforeEach(() => {
    vi.stubGlobal('localStorage', createMemoryStorage())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('has no view until one is remembered, then keeps it per trip', () => {
    expect(lastUsedView('trip-a')).toBeNull()

    rememberView('trip-a', 'table')
    rememberView('trip-b', 'board')

    expect(lastUsedView('trip-a')).toBe('table')
    expect(lastUsedView('trip-b')).toBe('board')
  })

  it('ignores storage errors', () => {
    vi.stubGlobal('localStorage', {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      },
    })

    expect(() => rememberView('trip-a', 'table')).not.toThrow()
    expect(lastUsedView('trip-a')).toBeNull()
  })
})

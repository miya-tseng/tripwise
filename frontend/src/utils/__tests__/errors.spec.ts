import { describe, expect, it } from 'vitest'
import { errorMessage, GENERIC_ERROR } from '../errors'
import { newSessionToken } from '../sessionToken'

describe('errorMessage', () => {
  it('uses the message of an error and a fallback otherwise', () => {
    expect(errorMessage(new Error('Trip not found.'))).toBe('Trip not found.')
    expect(errorMessage('boom')).toBe(GENERIC_ERROR)
    expect(errorMessage(null, 'Place search failed.')).toBe('Place search failed.')
  })
})

describe('newSessionToken', () => {
  it('creates a different token each time', () => {
    expect(newSessionToken()).not.toBe(newSessionToken())
  })
})

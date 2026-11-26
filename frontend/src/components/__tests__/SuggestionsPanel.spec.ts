import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import type { Activity } from '@/types/activity'
import type { Suggestion } from '@/types/suggestion'
import SuggestionsPanel from '../SuggestionsPanel.vue'

const anchor: Activity = {
  id: 'senso-ji-id',
  name: 'Senso-ji',
  date: '2026-12-01',
  startTime: '10:00:00',
  endTime: '11:30:00',
  cost: 0,
  currency: 'TWD',
  costInTwd: 0,
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

function suggestion(name: string, minutes: number): Suggestion {
  return {
    place: {
      placeId: `${name}-id`,
      name,
      address: `${name} address`,
      latitude: 35.7,
      longitude: 139.8,
      googleMapsUri: `https://maps.google.com/?q=${name}`,
    },
    reason: `${name} is close by`,
    suggestedMinutes: minutes,
  }
}

function mountPanel(
  props: Partial<{ suggestions: Suggestion[]; loading: boolean; error: string | null }> = {},
) {
  return mount(SuggestionsPanel, {
    props: { anchor, suggestions: [], loading: false, error: null, ...props },
  })
}

describe('SuggestionsPanel', () => {
  it('names the place the ideas are for', () => {
    expect(mountPanel().find('h2').text()).toBe('Near Senso-ji')
  })

  it('shows a loading message', () => {
    const wrapper = mountPanel({ loading: true })

    expect(wrapper.find('[data-testid="suggestions-loading"]').text()).toContain(
      'Finding places people usually pair with Senso-ji',
    )
  })

  it('shows the error', () => {
    const wrapper = mountPanel({ error: 'Suggestions are unavailable right now.' })

    expect(wrapper.find('[data-testid="suggestions-error"]').text()).toBe(
      'Suggestions are unavailable right now.',
    )
  })

  it('says when there are no new ideas', () => {
    expect(mountPanel().find('[data-testid="suggestions-empty"]').text()).toBe(
      'No new ideas for this place yet.',
    )
  })

  it('lists each suggestion with its reason, duration and map link', () => {
    const wrapper = mountPanel({
      suggestions: [
        suggestion('Tokyo Skytree', 90),
        suggestion('Ueno Park', 45),
        suggestion('Meiji Jingu', 120),
      ],
    })

    const items = wrapper.findAll('[data-testid="suggestion"]')
    expect(items).toHaveLength(3)
    expect(items[0]!.find('[data-testid="suggestion-name"]').text()).toBe('Tokyo Skytree')
    expect(items[0]!.text()).toContain('Tokyo Skytree address')
    expect(items[0]!.find('[data-testid="suggestion-reason"]').text()).toBe(
      'Tokyo Skytree is close by',
    )
    expect(
      wrapper.findAll('[data-testid="suggestion-duration"]').map((item) => item.text()),
    ).toEqual(['~1 h 30 min', '~45 min', '~2 h'])
    const link = items[0]!.find('[data-testid="suggestion-map-link"]')
    expect(link.attributes('href')).toBe('https://maps.google.com/?q=Tokyo Skytree')
    expect(link.attributes('target')).toBe('_blank')
    expect(wrapper.text()).toContain('Suggested by AI, matched to real places on Google.')
  })

  it('emits add with the suggestion', async () => {
    const skytree = suggestion('Tokyo Skytree', 90)
    const wrapper = mountPanel({ suggestions: [skytree] })

    await wrapper.find('[data-testid="add-suggestion"]').trigger('click')

    expect(wrapper.emitted('add')).toEqual([[skytree]])
  })

  it('emits close', async () => {
    const wrapper = mountPanel()

    await wrapper.find('[data-testid="close-suggestions"]').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })
})

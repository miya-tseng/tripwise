import { afterEach, describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import FilterMenu from '../FilterMenu.vue'
import { CATEGORIES } from '@/utils/activityDetails'

const bookingOptions = [
  { value: '', label: 'All bookings' },
  { value: 'ToBook', label: 'To book' },
  { value: 'Paid', label: 'Paid' },
]
const categories = CATEGORIES.slice(0, 2)

let wrapper: VueWrapper | null = null

function mountMenu(props: Record<string, unknown> = {}) {
  wrapper = mount(FilterMenu, {
    props: { bookingOptions, booking: '', categories, category: null, ...props },
    attachTo: document.body,
  })
  return wrapper
}

describe('FilterMenu', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('keeps the filters closed behind one button', async () => {
    const menu = mountMenu()

    expect(menu.find('[data-testid="filter-panel"]').exists()).toBe(false)
    await menu.find('[data-testid="filter-button"]').trigger('click')

    expect(menu.find('[data-testid="booking-filter"]').exists()).toBe(true)
    expect(menu.findAll('.filter-chip').map((chip) => chip.text())).toEqual([
      'All',
      '🏯 Sightseeing',
      '🍜 Food',
    ])
  })

  it('emits the chosen category', async () => {
    const menu = mountMenu()
    await menu.find('[data-testid="filter-button"]').trigger('click')

    await menu.find('[data-testid="filter-Food"]').trigger('click')

    expect(menu.emitted('update:category')).toEqual([['Food']])
  })

  it('counts active filters and clears them', async () => {
    const menu = mountMenu({ booking: 'ToBook', category: 'Food' })

    expect(menu.find('[data-testid="filter-count"]').text()).toBe('2')
    await menu.find('[data-testid="filter-button"]').trigger('click')
    await menu.find('[data-testid="clear-filters"]').trigger('click')

    expect(menu.emitted('update:booking')).toEqual([['']])
    expect(menu.emitted('update:category')).toEqual([[null]])
  })

  it('closes when clicking elsewhere', async () => {
    const menu = mountMenu()
    await menu.find('[data-testid="filter-button"]').trigger('click')

    document.body.dispatchEvent(new Event('pointerdown', { bubbles: true }))
    await menu.vm.$nextTick()

    expect(menu.find('[data-testid="filter-panel"]').exists()).toBe(false)
  })

  it('is hidden when there is nothing to filter', () => {
    const menu = mountMenu({
      bookingOptions: bookingOptions.slice(0, 2),
      categories: categories.slice(0, 1),
    })

    expect(menu.find('[data-testid="filter-button"]').exists()).toBe(false)
  })
})

import { describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import TripList from '../TripList.vue'
import ConfirmDialog from '../ConfirmDialog.vue'
import type { Trip } from '@/types/trip'

const tokyo: Trip = {
  id: 'tokyo-id',
  name: 'Tokyo',
  startDate: '2026-12-01',
  endDate: '2026-12-05',
  days: 5,
}

const global = { stubs: { RouterLink: RouterLinkStub } }

function mountList(deletingId: string | null = null) {
  return mount(TripList, { props: { trips: [tokyo], deletingId }, global })
}

describe('TripList', () => {
  it('shows an empty state when there are no trips', () => {
    const wrapper = mount(TripList, { props: { trips: [] }, global })

    expect(wrapper.find('[data-testid="empty-state"]').text()).toBe('No trips yet.')
    expect(wrapper.findAll('[data-testid="trip-item"]')).toHaveLength(0)
  })

  it('shows the name, dates and days of each trip', () => {
    const wrapper = mountList()

    const item = wrapper.find('[data-testid="trip-item"]')
    expect(item.text()).toContain('Tokyo')
    expect(item.text()).toContain('Dec 1 – 5, 2026')
    expect(item.text()).toContain('5 days')
  })

  it('links the trip name to its schedule', () => {
    const wrapper = mountList()

    const link = wrapper.findComponent(RouterLinkStub)
    expect(link.text()).toBe('Tokyo')
    expect(link.props('to')).toEqual({ name: 'trip-schedule', params: { id: 'tokyo-id' } })
  })

  it('asks for confirmation before deleting', async () => {
    const wrapper = mountList()

    await wrapper.find('[data-testid="delete-button"]').trigger('click')

    const dialog = wrapper.findComponent(ConfirmDialog)
    expect(dialog.props('open')).toBe(true)
    expect(dialog.text()).toContain('Are you sure you want to delete "Tokyo"?')
    expect(wrapper.emitted('delete')).toBeUndefined()
  })

  it('emits delete and closes the dialog when the deletion is confirmed', async () => {
    const wrapper = mountList()

    await wrapper.find('[data-testid="delete-button"]').trigger('click')
    await wrapper.find('[data-testid="confirm-button"]').trigger('click')

    expect(wrapper.emitted('delete')).toEqual([['tokyo-id']])
    expect(wrapper.findComponent(ConfirmDialog).props('open')).toBe(false)
  })

  it('does not emit delete when the deletion is cancelled', async () => {
    const wrapper = mountList()

    await wrapper.find('[data-testid="delete-button"]').trigger('click')
    await wrapper.find('[data-testid="cancel-button"]').trigger('click')

    expect(wrapper.emitted('delete')).toBeUndefined()
    expect(wrapper.findComponent(ConfirmDialog).props('open')).toBe(false)
  })

  it('disables the delete button while the trip is being deleted', () => {
    const wrapper = mountList('tokyo-id')

    const button = wrapper.find('[data-testid="delete-button"]')
    expect(button.text()).toBe('Deleting...')
    expect(button.attributes('disabled')).toBeDefined()
  })
})

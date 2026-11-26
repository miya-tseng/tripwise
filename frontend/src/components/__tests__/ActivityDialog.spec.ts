import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ActivityDialog from '../ActivityDialog.vue'
import ActivityForm from '../ActivityForm.vue'
import type { Activity, SaveActivityInput } from '@/types/activity'

type Submit = (input: SaveActivityInput) => Promise<boolean>

const dates = ['2026-12-01', '2026-12-02']

const activity: Activity = {
  id: 'a',
  name: 'Senso-ji',
  date: '2026-12-02',
  startTime: '10:00:00',
  endTime: '11:00:00',
  cost: 0,
  currency: 'TWD',
  costInTwd: 0,
  category: 'Other',
  bookingStatus: 'NotNeeded',
  notes: '',
  confirmationCode: '',
  link: null,
}

function mountDialog(props: Partial<InstanceType<typeof ActivityDialog>['$props']> = {}) {
  return mount(ActivityDialog, {
    props: { open: true, dates, submit: vi.fn<Submit>().mockResolvedValue(true), ...props },
  })
}

describe('ActivityDialog', () => {
  it('is hidden when closed', () => {
    expect(mountDialog({ open: false }).find('[role="dialog"]').exists()).toBe(false)
  })

  it('adds to the chosen day', () => {
    const wrapper = mountDialog({ initialDate: '2026-12-02' })

    expect(wrapper.find('h2').text()).toBe('Add activity')
    expect(wrapper.text()).toContain('Day 2 · Wed, Dec 2')
    expect(wrapper.findComponent(ActivityForm).props('initialDate')).toBe('2026-12-02')
    expect(wrapper.findComponent(ActivityForm).props('showCancel')).toBe(true)
  })

  it('edits an activity', () => {
    const wrapper = mountDialog({ activity })

    expect(wrapper.find('h2').text()).toBe('Edit "Senso-ji"')
    expect(wrapper.findComponent(ActivityForm).props('activity')).toEqual(activity)
  })

  it('closes after a successful save', async () => {
    const submit = vi.fn<Submit>().mockResolvedValue(true)
    const wrapper = mountDialog({ submit })

    await wrapper.findComponent(ActivityForm).props('submit')({} as SaveActivityInput)
    await flushPromises()

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('stays open when saving fails', async () => {
    const wrapper = mountDialog({ submit: vi.fn<Submit>().mockResolvedValue(false) })

    await wrapper.findComponent(ActivityForm).props('submit')({} as SaveActivityInput)
    await flushPromises()

    expect(wrapper.emitted('close')).toBeUndefined()
  })

  it('shows the error and lets it be dismissed', async () => {
    const wrapper = mountDialog({ error: 'The activity overlaps with "Ueno Park"' })

    expect(wrapper.find('[data-testid="schedule-error"]').text()).toContain('overlaps')

    await wrapper.find('[data-testid="dismiss-error"]').trigger('click')

    expect(wrapper.emitted('dismissError')).toHaveLength(1)
  })

  it.each([
    ['close button', '[data-testid="close-activity-dialog"]', 'click'],
    ['backdrop', '[data-testid="activity-dialog-backdrop"]', 'click'],
  ])('closes with the %s', async (_, selector, event) => {
    const wrapper = mountDialog()

    await wrapper.find(selector).trigger(event)

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('closes with escape and the form cancel button', async () => {
    const wrapper = mountDialog()

    await wrapper.find('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await wrapper.find('[data-testid="cancel-edit"]').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(2)
  })
})

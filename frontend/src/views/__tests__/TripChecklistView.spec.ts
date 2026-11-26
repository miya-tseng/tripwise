import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { addChecklistItem, fetchChecklist, reorderChecklist } from '@/api/checklist'
import { fetchTrip } from '@/api/trips'
import ChecklistSectionCard from '@/components/ChecklistSectionCard.vue'
import type { Checklist } from '@/types/checklist'
import TripChecklistView from '../TripChecklistView.vue'

vi.mock('@/api/checklist')
vi.mock('@/api/trips')

const checklist: Checklist = {
  tripId: 'tokyo-id',
  done: 1,
  total: 3,
  items: [
    { id: 'a', text: 'Passport', section: 'ToPack', done: true, dueDate: null, position: 0 },
    { id: 'b', text: 'Insurance', section: 'ToBook', done: false, dueDate: null, position: 0 },
  ],
  bookings: [
    {
      kind: 'Activity',
      id: 'act',
      name: 'teamLab',
      date: '2026-12-02',
      bookingStatus: 'ToBook',
      done: false,
    },
  ],
}

async function mountView() {
  const wrapper = mount(TripChecklistView, {
    props: { id: 'tokyo-id', today: '2026-11-26' },
    global: { plugins: [createPinia()], stubs: { RouterLink: RouterLinkStub } },
  })
  await flushPromises()
  return wrapper
}

describe('TripChecklistView', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(fetchTrip).mockResolvedValue({
      id: 'tokyo-id',
      name: 'Tokyo',
      startDate: '2026-12-01',
      endDate: '2026-12-03',
      days: 3,
    })
  })

  it('shows the trip header with the checklist tab active', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="header-trip-name"]').text()).toBe('Tokyo')
    expect(wrapper.find('[data-testid="tab-checklist"]').attributes('aria-current')).toBe('page')
  })

  it('shows the progress and the three sections', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)

    const wrapper = await mountView()
    const sections = wrapper.findAllComponents(ChecklistSectionCard)

    expect(fetchChecklist).toHaveBeenCalledWith('tokyo-id')
    expect(wrapper.find('[data-testid="checklist-progress"]').text()).toBe('1 / 3 done')
    expect(wrapper.find('[role="progressbar"]').attributes('aria-valuenow')).toBe('1')
    expect(sections.map((section) => section.props('section').value)).toEqual([
      'ToBook',
      'ToPack',
      'Other',
    ])
    expect(sections[0]!.props('bookings')).toHaveLength(1)
    expect(sections[1]!.props('bookings')).toEqual([])
    expect(sections[0]!.props('items').map((item: { text: string }) => item.text)).toEqual([
      'Insurance',
    ])
  })

  it('offers packing suggestions that are not on the list yet', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)

    const wrapper = await mountView()
    const pack = wrapper.findAllComponents(ChecklistSectionCard)[1]!

    expect(pack.props('suggestions')).not.toContain('Passport')
    expect(pack.props('suggestions')).toContain('Phone charger')
  })

  it('adds an item to the section it was typed in', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    vi.mocked(addChecklistItem).mockResolvedValue(checklist)
    const wrapper = await mountView()

    wrapper.findAllComponents(ChecklistSectionCard)[2]!.vm.$emit('add', 'Exchange yen')
    await flushPromises()

    expect(addChecklistItem).toHaveBeenCalledWith('tokyo-id', {
      text: 'Exchange yen',
      section: 'Other',
      dueDate: null,
    })
  })

  it('reorders within the section', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    vi.mocked(reorderChecklist).mockResolvedValue(checklist)
    const wrapper = await mountView()

    wrapper.findAllComponents(ChecklistSectionCard)[1]!.vm.$emit('reorder', ['a'])
    await flushPromises()

    expect(reorderChecklist).toHaveBeenCalledWith('tokyo-id', 'ToPack', ['a'])
  })

  it('shows a change error', async () => {
    vi.mocked(fetchChecklist).mockResolvedValue(checklist)
    vi.mocked(addChecklistItem).mockRejectedValue(new Error('Checklist item text is required.'))
    const wrapper = await mountView()

    wrapper.findAllComponents(ChecklistSectionCard)[0]!.vm.$emit('add', 'x')
    await flushPromises()

    expect(wrapper.find('[data-testid="checklist-error"]').text()).toContain(
      'Checklist item text is required.',
    )
  })

  it('shows the load error', async () => {
    vi.mocked(fetchChecklist).mockRejectedValue(new Error('Trip not found.'))

    const wrapper = await mountView()

    expect(wrapper.find('[data-testid="checklist-load-error"]').text()).toBe('Trip not found.')
  })
})

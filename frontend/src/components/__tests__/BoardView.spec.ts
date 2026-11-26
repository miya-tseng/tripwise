import { describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import type { Activity, ScheduleDay } from '@/types/activity'
import BoardView from '../BoardView.vue'

function activity(id: string, date: string, startTime: string, endTime: string): Activity {
  return {
    id,
    name: id,
    date,
    startTime,
    endTime,
    cost: 0,
    currency: 'TWD',
    costInTwd: 0,
    category: 'Other',
    bookingStatus: 'NotNeeded',
    notes: '',
    confirmationCode: '',
    link: null,
  }
}

const morning = activity('Senso-ji', '2026-12-01', '09:00:00', '10:30:00')
const lunch = activity('Lunch', '2026-12-01', '12:00:00', '13:00:00')
const evening = activity('Tokyo Tower', '2026-12-02', '18:00:00', '19:00:00')

const days: ScheduleDay[] = [
  { date: '2026-12-01', activities: [morning, lunch] },
  { date: '2026-12-02', activities: [evening] },
  { date: '2026-12-03', activities: [] },
]

function mountBoard() {
  return mount(BoardView, { props: { days }, attachTo: document.body })
}

function column(wrapper: VueWrapper, index: number) {
  return wrapper.findAll('[data-testid="board-column"]')[index]!
}

function card(wrapper: VueWrapper, name: string) {
  return wrapper.findAll('[data-testid="board-card"]').find((item) => item.text().includes(name))!
}

function placeCards(wrapper: VueWrapper, columnIndex: number, tops: number[]) {
  column(wrapper, columnIndex)
    .findAll('[data-testid="board-card"]')
    .forEach((item, index) => {
      item.element.getBoundingClientRect = () => ({ top: tops[index]!, height: 40 }) as DOMRect
    })
}

async function dragTo(
  wrapper: VueWrapper,
  name: string,
  target: 'header' | 'list',
  columnIndex: number,
  clientY = 0,
) {
  await card(wrapper, name).trigger('dragstart')
  const zone = column(wrapper, columnIndex).find(
    target === 'header' ? '[data-testid="board-column-header"]' : '[data-testid="board-cards"]',
  )
  await zone.trigger('dragover', { clientY })
  await zone.trigger('drop', { clientY })
}

describe('BoardView', () => {
  it('shows one column per day with its cards in order', () => {
    const wrapper = mountBoard()

    expect(wrapper.findAll('[data-testid="board-column"]')).toHaveLength(3)
    expect(column(wrapper, 0).text()).toContain('Day 1')
    expect(column(wrapper, 0).text()).toContain('Tue, Dec 1')
    expect(
      column(wrapper, 0)
        .findAll('[data-testid="board-card-name"]')
        .map((item) => item.text()),
    ).toEqual(['Senso-ji', 'Lunch'])
    expect(column(wrapper, 0).find('[data-testid="board-card-time"]').text()).toBe('09:00 – 10:30')
    expect(column(wrapper, 2).find('[data-testid="board-column-empty"]').text()).toBe(
      'Drop an activity here',
    )
    wrapper.unmount()
  })

  it('keeps the start time when dropped on another day header', async () => {
    const wrapper = mountBoard()

    await dragTo(wrapper, 'Senso-ji', 'header', 1)

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Senso-ji', date: '2026-12-02', startTime: '09:00' }],
    ])
    wrapper.unmount()
  })

  it('keeps the start time when dropped on an empty day', async () => {
    const wrapper = mountBoard()

    await dragTo(wrapper, 'Lunch', 'list', 2)

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Lunch', date: '2026-12-03', startTime: '12:00' }],
    ])
    wrapper.unmount()
  })

  it('starts right after the card above when dropped between cards', async () => {
    const wrapper = mountBoard()
    placeCards(wrapper, 1, [100])

    await dragTo(wrapper, 'Senso-ji', 'list', 1, 200)

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Senso-ji', date: '2026-12-02', startTime: '19:00' }],
    ])
    wrapper.unmount()
  })

  it('ends before the first card when dropped at the top', async () => {
    const wrapper = mountBoard()
    placeCards(wrapper, 1, [100])

    await dragTo(wrapper, 'Senso-ji', 'list', 1, 50)

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Senso-ji', date: '2026-12-02', startTime: '16:30' }],
    ])
    wrapper.unmount()
  })

  it('reorders within the same day', async () => {
    const wrapper = mountBoard()
    placeCards(wrapper, 0, [100, 200])

    await dragTo(wrapper, 'Senso-ji', 'list', 0, 300)

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Senso-ji', date: '2026-12-01', startTime: '13:00' }],
    ])
    wrapper.unmount()
  })

  it('does nothing when dropped back where it was', async () => {
    const wrapper = mountBoard()

    await dragTo(wrapper, 'Senso-ji', 'header', 0)

    expect(wrapper.emitted('move')).toBeUndefined()
    wrapper.unmount()
  })

  it('shows where the card will land while dragging', async () => {
    const wrapper = mountBoard()
    placeCards(wrapper, 0, [100, 200])

    await card(wrapper, 'Tokyo Tower').trigger('dragstart')
    await column(wrapper, 0)
      .find('[data-testid="board-cards"]')
      .trigger('dragover', { clientY: 150 })

    const list = column(wrapper, 0).find('[data-testid="board-cards"]')
    const items = list.findAll(':scope > li').map((item) => item.attributes('data-testid'))
    expect(items).toEqual(['board-card', 'drop-indicator', 'board-card'])
    expect(column(wrapper, 0).classes()).toContain('drop-target')
    wrapper.unmount()
  })

  it('moves with the keyboard menu', async () => {
    const wrapper = mountBoard()

    await card(wrapper, 'Lunch').find('[data-testid="move-menu"]').trigger('click')
    await card(wrapper, 'Lunch').find('[data-testid="move-later"]').trigger('click')
    await card(wrapper, 'Lunch').find('[data-testid="move-menu"]').trigger('click')
    await card(wrapper, 'Lunch').find('[data-testid="move-earlier"]').trigger('click')
    await card(wrapper, 'Lunch').find('[data-testid="move-menu"]').trigger('click')
    await card(wrapper, 'Lunch').find('[data-testid="move-next-day"]').trigger('click')

    expect(wrapper.emitted('move')).toEqual([
      [{ activityId: 'Lunch', date: '2026-12-01', startTime: '12:30' }],
      [{ activityId: 'Lunch', date: '2026-12-01', startTime: '11:30' }],
      [{ activityId: 'Lunch', date: '2026-12-02', startTime: '12:00' }],
    ])
    wrapper.unmount()
  })

  it('disables moving before the first day or after the last day', async () => {
    const wrapper = mountBoard()

    await card(wrapper, 'Senso-ji').find('[data-testid="move-menu"]').trigger('click')

    expect(
      card(wrapper, 'Senso-ji').find('[data-testid="move-previous-day"]').attributes('disabled'),
    ).toBeDefined()
    expect(
      card(wrapper, 'Senso-ji').find('[data-testid="move-next-day"]').attributes('disabled'),
    ).toBeUndefined()
    wrapper.unmount()
  })

  it('labels the move button for screen readers', () => {
    const wrapper = mountBoard()

    expect(card(wrapper, 'Lunch').find('[data-testid="move-menu"]').attributes('aria-label')).toBe(
      'Move Lunch',
    )
    wrapper.unmount()
  })

  it('opens the activity for editing when its name is clicked', async () => {
    const wrapper = mountBoard()

    await card(wrapper, 'Lunch').find('[data-testid="board-card-name"]').trigger('click')

    expect(wrapper.emitted('edit')).toEqual([[lunch]])
    wrapper.unmount()
  })

  it('shows the category and booking status on cards', () => {
    const wrapper = mount(BoardView, {
      props: {
        days: [
          {
            date: '2026-12-01',
            activities: [{ ...morning, category: 'Sightseeing', bookingStatus: 'ToBook' }],
          },
        ],
      },
    })

    const boardCard = wrapper.find('[data-testid="board-card"]')
    expect(boardCard.classes()).toContain('category-sightseeing')
    expect(boardCard.text()).toContain('🏯 Sightseeing')
    expect(boardCard.find('[data-testid="board-booking-badge"]').text()).toBe('To book')
  })
})

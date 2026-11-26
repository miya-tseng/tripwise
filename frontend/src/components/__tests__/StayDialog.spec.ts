import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import StayDialog from '../StayDialog.vue'
import ConfirmDialog from '../ConfirmDialog.vue'
import type { SaveStayInput, Stay } from '@/types/stay'

const dates = ['2026-12-01', '2026-12-02', '2026-12-03']

const hotel: Stay = {
  id: 'hotel',
  name: 'Hotel Gracery',
  checkIn: '2026-12-01',
  checkOut: '2026-12-03',
  nights: 2,
  cost: 9000,
  currency: 'TWD',
  costInTwd: 9000,
  bookingStatus: 'Booked',
  confirmationCode: 'HG-1',
}

function mountDialog(props: Record<string, unknown> = {}) {
  const submit = vi.fn<(input: SaveStayInput) => Promise<boolean>>().mockResolvedValue(true)
  const wrapper = mount(StayDialog, {
    props: { open: true, dates, submit, ...props },
    attachTo: document.body,
  })
  return { wrapper, submit }
}

async function chooseOption(wrapper: ReturnType<typeof mount>, testId: string, label: string) {
  const select = wrapper.find(`[data-testid="${testId}"]`)
  await select.find('[data-testid="select-trigger"]').trigger('click')
  const option = select.findAll('[role="option"]').find((item) => item.text().startsWith(label))
  await option!.trigger('click')
}

describe('StayDialog', () => {
  it('starts with the given check-in and the next day as check-out', () => {
    const { wrapper } = mountDialog({ initialCheckIn: '2026-12-02', initialCheckOut: '2026-12-03' })

    expect(wrapper.find('[data-testid="check-in-select"]').text()).toContain('Wed, Dec 2')
    expect(wrapper.find('[data-testid="check-out-select"]').text()).toContain('Thu, Dec 3')
    expect(wrapper.find('[data-testid="stay-nights"]').text()).toBe('1 night')
    wrapper.unmount()
  })

  it('offers check-out dates after check-in, including the day after the trip', async () => {
    const { wrapper } = mountDialog({ initialCheckIn: '2026-12-02', initialCheckOut: '2026-12-03' })

    await wrapper
      .find('[data-testid="check-out-select"] [data-testid="select-trigger"]')
      .trigger('click')
    const options = wrapper.findAll('[data-testid="check-out-select"] [role="option"]')

    expect(options.map((option) => option.text())).toEqual([
      'Thu, Dec 31 night',
      'Fri, Dec 42 nights',
    ])
    wrapper.unmount()
  })

  it('moves check-out forward when check-in passes it', async () => {
    const { wrapper } = mountDialog({ initialCheckIn: '2026-12-01', initialCheckOut: '2026-12-02' })

    await chooseOption(wrapper, 'check-in-select', 'Thu, Dec 3')

    expect(wrapper.find('[data-testid="check-out-select"]').text()).toContain('Fri, Dec 4')
    wrapper.unmount()
  })

  it('saves a typed stay with its cost and booking', async () => {
    const { wrapper, submit } = mountDialog({
      initialCheckIn: '2026-12-01',
      initialCheckOut: '2026-12-03',
    })

    await wrapper.find('[data-testid="use-custom-stay"]').trigger('click')
    await wrapper.find('input[name="stayName"]').setValue('Airbnb Shinjuku')
    await wrapper.find('input[name="stayCost"]').setValue('9000')
    await chooseOption(wrapper, 'stay-booking-select', 'Booked')
    await wrapper.find('input[name="stayConfirmationCode"]').setValue(' AB-1 ')
    await wrapper.find('[data-testid="stay-form"]').trigger('submit')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith({
      name: 'Airbnb Shinjuku',
      checkIn: '2026-12-01',
      checkOut: '2026-12-03',
      cost: 9000,
      currency: 'TWD',
      bookingStatus: 'Booked',
      confirmationCode: 'AB-1',
    })
    expect(wrapper.emitted('close')).toHaveLength(1)
    wrapper.unmount()
  })

  it('shows the cost per night', async () => {
    const { wrapper } = mountDialog({ initialCheckIn: '2026-12-01', initialCheckOut: '2026-12-03' })

    await wrapper.find('input[name="stayCost"]').setValue('9000')

    expect(wrapper.find('[data-testid="stay-cost-preview"]').text()).toBe('NT$4,500.00 / night')
    wrapper.unmount()
  })

  it('cannot be saved without a name', () => {
    const { wrapper } = mountDialog()

    expect(wrapper.find('[data-testid="save-stay"]').attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('fills the form when editing', () => {
    const { wrapper } = mountDialog({ stay: hotel })

    expect(wrapper.text()).toContain('Edit "Hotel Gracery"')
    expect((wrapper.find('input[name="stayName"]').element as HTMLInputElement).value).toBe(
      'Hotel Gracery',
    )
    expect((wrapper.find('input[name="stayCost"]').element as HTMLInputElement).value).toBe(
      '9000.00',
    )
    expect(wrapper.find('[data-testid="stay-nights"]').text()).toBe('2 nights')
    wrapper.unmount()
  })

  it('stays open and shows the error when saving fails', async () => {
    const { wrapper, submit } = mountDialog({ stay: hotel, error: 'These nights overlap.' })
    submit.mockResolvedValue(false)

    await wrapper.find('[data-testid="stay-form"]').trigger('submit')
    await flushPromises()

    expect(wrapper.emitted('close')).toBeUndefined()
    expect(wrapper.find('[data-testid="stay-error"]').text()).toContain('These nights overlap.')
    wrapper.unmount()
  })

  it('deletes after confirming', async () => {
    const remove = vi.fn<(id: string) => Promise<boolean>>().mockResolvedValue(true)
    const { wrapper } = mountDialog({ stay: hotel, remove })

    await wrapper.find('[data-testid="delete-stay"]').trigger('click')
    wrapper.findComponent(ConfirmDialog).vm.$emit('confirm')
    await flushPromises()

    expect(remove).toHaveBeenCalledWith('hotel')
    expect(wrapper.emitted('close')).toHaveLength(1)
    wrapper.unmount()
  })

  it('sets the check-out from a quick choice up to the next stay', async () => {
    const fourDays = ['2026-12-01', '2026-12-02', '2026-12-03', '2026-12-04', '2026-12-05']
    const later: Stay = { ...hotel, id: 'later', checkIn: '2026-12-04', checkOut: '2026-12-06' }
    const { wrapper } = mountDialog({
      dates: fourDays,
      stays: [later],
      initialCheckIn: '2026-12-01',
      initialCheckOut: '2026-12-02',
    })
    const chips = wrapper.findAll('[data-testid="quick-check-out"]')

    expect(chips.map((chip) => chip.text())).toEqual(['1 night', '2 nights', 'All 3 nights'])
    expect(chips[0]!.attributes('aria-pressed')).toBe('true')

    await chips[2]!.trigger('click')

    expect(wrapper.find('[data-testid="check-out-select"]').text()).toContain('Fri, Dec 4')
    expect(wrapper.find('[data-testid="stay-nights"]').text()).toBe('3 nights')
    expect(chips[2]!.attributes('aria-pressed')).toBe('true')
    wrapper.unmount()
  })

  it('ignores the stay being edited when offering quick choices', () => {
    const { wrapper } = mountDialog({ stay: hotel, stays: [hotel] })

    expect(wrapper.findAll('[data-testid="quick-check-out"]').map((chip) => chip.text())).toEqual([
      '1 night',
      '2 nights',
      'All 3 nights',
    ])
    wrapper.unmount()
  })

  it('hides quick choices when only one night is possible', () => {
    const { wrapper } = mountDialog({ initialCheckIn: '2026-12-03', initialCheckOut: '2026-12-04' })

    expect(wrapper.find('[data-testid="quick-check-out"]').exists()).toBe(false)
    wrapper.unmount()
  })
})

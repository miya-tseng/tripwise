import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ConfirmDialog from '../ConfirmDialog.vue'

function mountDialog(open: boolean) {
  return mount(ConfirmDialog, {
    props: {
      open,
      title: 'Delete trip?',
      message: 'Are you sure you want to delete "Tokyo"?',
      confirmLabel: 'Delete',
    },
  })
}

describe('ConfirmDialog', () => {
  it('renders nothing when closed', () => {
    const wrapper = mountDialog(false)

    expect(wrapper.find('[role="alertdialog"]').exists()).toBe(false)
  })

  it('shows the title, message and buttons when open', () => {
    const wrapper = mountDialog(true)

    const dialog = wrapper.find('[role="alertdialog"]')
    expect(dialog.text()).toContain('Delete trip?')
    expect(dialog.text()).toContain('Are you sure you want to delete "Tokyo"?')
    expect(wrapper.find('[data-testid="confirm-button"]').text()).toBe('Delete')
    expect(wrapper.find('[data-testid="cancel-button"]').text()).toBe('Cancel')
  })

  it('emits confirm when the confirm button is clicked', async () => {
    const wrapper = mountDialog(true)

    await wrapper.find('[data-testid="confirm-button"]').trigger('click')

    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('emits cancel when the cancel button is clicked', async () => {
    const wrapper = mountDialog(true)

    await wrapper.find('[data-testid="cancel-button"]').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('emits cancel when escape is pressed', async () => {
    const wrapper = mountDialog(true)

    await wrapper.find('[role="alertdialog"]').trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('emits cancel when the backdrop is clicked', async () => {
    const wrapper = mountDialog(true)

    await wrapper.find('[data-testid="dialog-backdrop"]').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })
})

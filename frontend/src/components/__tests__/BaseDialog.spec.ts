import { describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { mount } from '@vue/test-utils'
import BaseDialog from '../BaseDialog.vue'

function mountDialog(props: Record<string, unknown> = {}) {
  return mount(BaseDialog, {
    props: {
      open: true,
      labelledby: 'title',
      testid: 'panel',
      backdropTestid: 'backdrop',
      ...props,
    },
    slots: { default: '<h2 id="title">Hello</h2><button type="submit">Go</button>' },
  })
}

describe('BaseDialog', () => {
  it('renders nothing while closed', () => {
    expect(mountDialog({ open: false }).find('[data-testid="panel"]').exists()).toBe(false)
  })

  it('is a labelled modal dialog with the given role and width', () => {
    const panel = mountDialog({ role: 'alertdialog', describedby: 'msg', width: '400px' }).find(
      '[data-testid="panel"]',
    )

    expect(panel.attributes('role')).toBe('alertdialog')
    expect(panel.attributes('aria-modal')).toBe('true')
    expect(panel.attributes('aria-labelledby')).toBe('title')
    expect(panel.attributes('aria-describedby')).toBe('msg')
    expect((panel.element as HTMLElement).style.maxWidth).toBe('400px')
    expect(panel.text()).toContain('Hello')
  })

  it('closes on Escape and on a click on the backdrop, not inside', async () => {
    const wrapper = mountDialog()

    await wrapper.find('[data-testid="panel"]').trigger('click')
    expect(wrapper.emitted('close')).toBeUndefined()

    await wrapper.find('[data-testid="backdrop"]').trigger('click')
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('close')).toHaveLength(2)
    wrapper.unmount()
  })

  it('moves focus into the dialog when it opens', async () => {
    const wrapper = mount(BaseDialog, {
      props: { open: false, labelledby: 'title', testid: 'panel' },
      slots: { default: '<h2 id="title">Hello</h2>' },
      attachTo: document.body,
    })

    await wrapper.setProps({ open: true })
    await nextTick()

    expect(document.activeElement).toBe(wrapper.find('[data-testid="panel"]').element)
    wrapper.unmount()
  })

  it('lets only the top dialog close on Escape', async () => {
    const lower = mountDialog({ testid: 'lower' })
    const upper = mountDialog({ testid: 'upper' })

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))

    expect(upper.emitted('close')).toHaveLength(1)
    expect(lower.emitted('close')).toBeUndefined()

    upper.unmount()
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(lower.emitted('close')).toHaveLength(1)
    lower.unmount()
  })

  it('emits submit when rendered as a form', async () => {
    const wrapper = mountDialog({ tag: 'form' })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')).toHaveLength(1)
  })
})

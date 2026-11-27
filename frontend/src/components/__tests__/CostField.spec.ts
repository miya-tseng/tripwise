import { afterEach, describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import CostField from '../CostField.vue'

let wrapper: VueWrapper | null = null

function mountField(props: Record<string, unknown> = {}) {
  wrapper = mount(CostField, {
    props: {
      label: 'Cost',
      amountName: 'cost',
      currencyFieldName: 'currency',
      currencyTestid: 'currency-select',
      previewTestid: 'cost-preview',
      errorTestid: 'cost-error',
      amount: '',
      currency: 'TWD',
      'onUpdate:amount': (value: string) => wrapper!.setProps({ amount: value }),
      ...props,
    },
  })
  return wrapper
}

describe('CostField', () => {
  afterEach(() => {
    wrapper?.unmount()
    wrapper = null
  })

  it('updates the amount as the user types', async () => {
    const field = mountField()

    await field.get('input[name="cost"]').setValue('12.50')

    expect(field.emitted('update:amount')).toEqual([['12.50']])
  })

  it('offers the currencies the rates cover', () => {
    const field = mountField({
      rates: {
        baseCurrency: 'TWD',
        available: true,
        updatedAt: null,
        currencies: ['TWD', 'JPY'],
        unitsPerBaseCurrency: { TWD: 1, JPY: 4.7 },
      },
    })

    const options = field.findComponent({ name: 'SelectMenu' }).props('options') as {
      value: string
    }[]

    expect(options.map((option) => option.value)).toEqual(['TWD', 'JPY'])
  })

  it('shows the preview only when there is one', async () => {
    const field = mountField()

    expect(field.find('[data-testid="cost-preview"]').exists()).toBe(false)

    await field.setProps({ preview: '≈ NT$100.00' })

    expect(field.get('[data-testid="cost-preview"]').text()).toBe('≈ NT$100.00')
  })

  it('marks the amount invalid and shows the error', () => {
    const field = mountField({ error: 'Enter a valid amount.' })

    expect(field.get('.cost-row').classes()).toContain('invalid')
    expect(field.get('input[name="cost"]').attributes('aria-invalid')).toBe('true')
    expect(field.get('[data-testid="cost-error"]').text()).toBe('Enter a valid amount.')
  })

  it('uses the label for the amount input', () => {
    const field = mountField({ label: 'Total cost' })

    expect(field.get('input').attributes('aria-label')).toBe('Total cost')
    expect(field.get('.field-label').text()).toContain('Total cost')
  })
})

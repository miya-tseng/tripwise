<script setup lang="ts">
import { computed } from 'vue'
import SelectMenu from '@/components/SelectMenu.vue'
import type { ExchangeRates } from '@/types/currency'
import { currencySelectOptions } from '@/utils/money'

const props = withDefaults(
  defineProps<{
    label: string
    amountName: string
    currencyFieldName: string
    currencyTestid: string
    previewTestid: string
    errorTestid: string
    rates?: ExchangeRates | null
    preview?: string
    error?: string
  }>(),
  {
    rates: null,
    preview: '',
    error: '',
  },
)

const amount = defineModel<string>('amount', { required: true })
const currency = defineModel<string>('currency', { required: true })

const currencyOptions = computed(() => currencySelectOptions(currency.value, props.rates))
</script>

<template>
  <div class="field">
    <span class="field-label">
      {{ label }}
      <span class="field-optional">Optional</span>
    </span>
    <div class="cost-row" :class="{ invalid: error }">
      <input
        v-model="amount"
        class="text-input cost-amount"
        :name="amountName"
        type="text"
        inputmode="decimal"
        placeholder="0.00"
        autocomplete="off"
        :aria-label="label"
        :aria-invalid="error !== ''"
      />
      <SelectMenu
        v-model="currency"
        class="cost-currency"
        align="end"
        :name="currencyFieldName"
        :options="currencyOptions"
        :data-testid="currencyTestid"
      />
    </div>
    <span v-if="preview" class="cost-preview" :data-testid="previewTestid">
      {{ preview }}
    </span>
    <p v-if="error" class="field-error" :data-testid="errorTestid">{{ error }}</p>
  </div>
</template>

<style scoped>
.field-optional {
  font-weight: 500;
  color: var(--color-subtle);
}

.field-error {
  font-size: 0.85rem;
  color: var(--color-danger);
}

.cost-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 7.5rem;
  gap: 0.5rem;
}

.cost-amount {
  font-variant-numeric: tabular-nums;
}

.cost-row.invalid .cost-amount {
  border-color: var(--color-danger);
}

.cost-row.invalid .cost-amount:focus {
  box-shadow: 0 0 0 4px rgb(179 65 47 / 0.15);
}

.cost-currency :deep(.select-menu) {
  width: 15rem;
}

.cost-currency :deep(.select-hint) {
  display: none;
}

.cost-preview {
  font-size: 0.85rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-primary);
}
</style>

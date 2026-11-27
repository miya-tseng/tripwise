<script setup lang="ts">
import type { Expense } from '@/types/overview'
import { formatDayLabel } from '@/utils/date'
import { formatCurrency, formatTwd } from '@/utils/money'

defineProps<{
  expenses: Expense[]
}>()

function twd(amount: number | null): string {
  return amount === null ? '—' : formatTwd(amount)
}
</script>

<template>
  <div class="panel-content">
    <ol v-if="expenses.length > 0" class="expense-list">
      <li v-for="expense in expenses" :key="expense.id" data-testid="top-expense">
        <span class="expense-icon" aria-hidden="true">{{
          expense.kind === 'Stay' ? '🛏' : '🎟️'
        }}</span>
        <span class="expense-text">
          <span class="expense-name">{{ expense.name }}</span>
          <span class="expense-date">{{ formatDayLabel(expense.date) }}</span>
        </span>
        <span class="expense-amount">
          {{ formatCurrency(expense.cost.amount, expense.cost.currency) }}
          <small v-if="expense.cost.currency !== 'TWD'">≈ {{ twd(expense.costInTwd) }}</small>
        </span>
      </li>
    </ol>
    <p v-else class="muted">No costs yet.</p>
  </div>
</template>

<style scoped>
.panel-content {
  display: contents;
}

.muted {
  font-size: 0.88rem;
  color: var(--color-muted);
}

.expense-list {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.expense-list li {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.expense-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.expense-name {
  overflow: hidden;
  font-size: 0.9rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.expense-date {
  font-size: 0.76rem;
  color: var(--color-muted);
}

.expense-amount {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  font-size: 0.88rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.expense-amount small {
  font-size: 0.74rem;
  font-weight: 400;
  color: var(--color-muted);
}
</style>

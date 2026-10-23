import { Expense, ExpenseList } from '../models/expense.models';

let next = 0;

export function testExpense(overrides: Partial<Expense> = {}): Expense {
  next++;
  return {
    id: `00000000-0000-4000-8000-${String(next).padStart(12, '0')}`,
    description: `Expense ${next}`,
    amount: 10,
    category: 'Food',
    date: '2026-10-20',
    createdAtUtc: '2026-10-20T15:00:00Z',
    updatedAtUtc: '2026-10-20T15:00:00Z',
    ...overrides,
  };
}

export function testList(items: Expense[], overrides: Partial<ExpenseList> = {}): ExpenseList {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalAmount: items.reduce((sum, e) => sum + e.amount, 0),
    ...overrides,
  };
}

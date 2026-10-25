// Shapes of the Expenses.Api contract (AGENTS.md 9.5 and 10.9). Field names match the camelCase JSON.

/** A calendar date as the API sends it: "yyyy-MM-dd", no time, no time zone. */
export type IsoDate = string;

export interface Expense {
  readonly id: string;
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly date: IsoDate;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

/** Body of POST and PUT. The owner is never sent: the API takes it from the token. */
export interface ExpenseInput {
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly date: IsoDate;
}

export interface ExpenseFilters {
  readonly category: string | null;
  readonly from: IsoDate | null;
  readonly to: IsoDate | null;
}

export interface ExpenseListQuery extends ExpenseFilters {
  readonly page: number;
  readonly pageSize: number;
}

/** One page plus totals over every expense matching the filters. */
export interface ExpenseList {
  readonly items: readonly Expense[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalAmount: number;
}

export interface Category {
  readonly value: string;
  readonly label: string;
}

export const noFilters: ExpenseFilters = { category: null, from: null, to: null };

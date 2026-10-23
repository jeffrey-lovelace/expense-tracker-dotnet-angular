import { computed, inject, Injectable, Signal, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { Expense, ExpenseFilters, ExpenseInput, noFilters } from '../models/expense.models';
import { ExpensesApiService } from '../services/expenses-api.service';

/**
 * The expense list: current page, filters, totals, loading and error state (AGENTS.md 11.1). Private writable
 * signals, public read-only signals. Requests go through one RxJS pipeline with switchMap, so a newer query
 * cancels an older one and a slow response can never overwrite a newer result.
 */
@Injectable({ providedIn: 'root' })
export class ExpensesStore {
  private readonly api = inject(ExpensesApiService);
  private readonly auth = inject(AuthStore);

  private readonly state = signal({
    items: [] as readonly Expense[],
    totalCount: 0,
    totalAmount: 0,
    page: 1,
    pageSize: 20,
    filters: noFilters,
    loading: false,
    error: false,
  });

  readonly items: Signal<readonly Expense[]> = computed(() => this.state().items);
  readonly totalCount: Signal<number> = computed(() => this.state().totalCount);
  readonly totalAmount: Signal<number> = computed(() => this.state().totalAmount);
  readonly page: Signal<number> = computed(() => this.state().page);
  readonly pageSize: Signal<number> = computed(() => this.state().pageSize);
  readonly filters: Signal<ExpenseFilters> = computed(() => this.state().filters);
  readonly loading: Signal<boolean> = computed(() => this.state().loading);
  readonly error: Signal<boolean> = computed(() => this.state().error);

  load(): void {
    throw new Error(`Not implemented ${this.api} ${this.auth}`);
  }

  setFilters(filters: ExpenseFilters): void {
    throw new Error(`Not implemented ${filters.category}`);
  }

  setPage(page: number, pageSize: number): void {
    throw new Error(`Not implemented ${page} ${pageSize}`);
  }

  create(input: ExpenseInput): Observable<Expense> {
    throw new Error(`Not implemented ${input.description}`);
  }

  update(id: string, input: ExpenseInput): Observable<Expense> {
    throw new Error(`Not implemented ${id} ${input.description}`);
  }

  remove(id: string): Observable<void> {
    throw new Error(`Not implemented ${id}`);
  }
}

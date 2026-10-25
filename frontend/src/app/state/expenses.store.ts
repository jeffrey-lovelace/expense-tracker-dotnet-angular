import { computed, effect, inject, Injectable, Signal, signal, untracked } from '@angular/core';
import { catchError, map, Observable, of, Subject, switchMap, tap } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import {
  Expense,
  ExpenseFilters,
  ExpenseInput,
  ExpenseList,
  ExpenseListQuery,
  noFilters,
} from '../models/expense.models';
import { ExpensesApiService } from '../services/expenses-api.service';

interface ExpensesState {
  readonly items: readonly Expense[];
  readonly totalCount: number;
  readonly totalAmount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly filters: ExpenseFilters;
  readonly loading: boolean;
  readonly error: boolean;
}

const initialState: ExpensesState = {
  items: [],
  totalCount: 0,
  totalAmount: 0,
  page: 1,
  pageSize: 20,
  filters: noFilters,
  loading: false,
  error: false,
};

/**
 * The expense list: current page, filters, totals, loading and error state (AGENTS.md 11.1). Private writable
 * signal, public read-only signals. Requests go through one RxJS pipeline with switchMap, so a newer query
 * cancels an older one and a slow response can never overwrite a newer result.
 */
@Injectable({ providedIn: 'root' })
export class ExpensesStore {
  private readonly api = inject(ExpensesApiService);
  private readonly auth = inject(AuthStore);
  private readonly state = signal<ExpensesState>(initialState);
  private readonly queries = new Subject<ExpenseListQuery>();

  readonly items: Signal<readonly Expense[]> = computed(() => this.state().items);
  readonly totalCount: Signal<number> = computed(() => this.state().totalCount);
  readonly totalAmount: Signal<number> = computed(() => this.state().totalAmount);
  readonly page: Signal<number> = computed(() => this.state().page);
  readonly pageSize: Signal<number> = computed(() => this.state().pageSize);
  readonly filters: Signal<ExpenseFilters> = computed(() => this.state().filters);
  readonly loading: Signal<boolean> = computed(() => this.state().loading);
  readonly error: Signal<boolean> = computed(() => this.state().error);

  constructor() {
    this.queries
      .pipe(
        switchMap((query) =>
          this.api.list(query).pipe(
            map((list): ExpenseList | null => list),
            // Caught inside switchMap: a failed request must not end the pipeline for the next ones.
            catchError(() => of(null)),
          ),
        ),
      )
      .subscribe((list) => this.receive(list));

    // A different user (or sign-out) must never see the previous user's cached list.
    let previousUserId = untracked(() => this.auth.user()?.id ?? null);
    effect(() => {
      const userId = this.auth.user()?.id ?? null;
      if (userId !== previousUserId) {
        previousUserId = userId;
        untracked(() => this.reset());
      }
    });
  }

  /** Reloads the current page with the current filters. */
  load(): void {
    const { page, pageSize, filters } = this.state();
    this.state.update((s) => ({ ...s, loading: true, error: false }));
    this.queries.next({ page, pageSize, ...filters });
  }

  setFilters(filters: ExpenseFilters): void {
    this.state.update((s) => ({ ...s, filters, page: 1 }));
    this.load();
  }

  setPage(page: number, pageSize: number): void {
    this.state.update((s) => ({ ...s, page, pageSize }));
    this.load();
  }

  /** Saves, then reloads so order, totals and paging come from the server. Errors go back to the caller. */
  create(input: ExpenseInput): Observable<Expense> {
    return this.api.create(input).pipe(tap(() => this.load()));
  }

  update(id: string, input: ExpenseInput): Observable<Expense> {
    return this.api.update(id, input).pipe(tap(() => this.load()));
  }

  remove(id: string): Observable<void> {
    return this.api.remove(id).pipe(
      tap(() => {
        // Deleting the only row of the last page would leave an empty page: step back one.
        const { items, page } = this.state();
        if (items.length === 1 && page > 1) {
          this.state.update((s) => ({ ...s, page: page - 1 }));
        }

        this.load();
      }),
    );
  }

  private receive(list: ExpenseList | null): void {
    if (list === null) {
      this.state.update((s) => ({ ...s, loading: false, error: true }));
      return;
    }

    this.state.update((s) => ({
      ...s,
      items: list.items,
      totalCount: list.totalCount,
      totalAmount: list.totalAmount,
      loading: false,
      error: false,
    }));
  }

  private reset(): void {
    this.state.set(initialState);
  }
}

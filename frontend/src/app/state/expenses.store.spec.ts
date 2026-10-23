import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { User } from '../models/auth.models';
import { ExpenseList } from '../models/expense.models';
import { ExpensesApiService } from '../services/expenses-api.service';
import { testExpense, testList } from '../testing/expense-data';
import { ExpensesStore } from './expenses.store';

describe('ExpensesStore', () => {
  const demo: User = { id: 'demo', email: 'demo@ballastlane.com', displayName: 'Demo User' };
  let user: ReturnType<typeof signal<User | null>>;
  let api: jasmine.SpyObj<ExpensesApiService>;
  let store: ExpensesStore;

  beforeEach(() => {
    user = signal<User | null>(demo);
    api = jasmine.createSpyObj<ExpensesApiService>('ExpensesApiService', [
      'list',
      'get',
      'create',
      'update',
      'remove',
    ]);
    api.list.and.returnValue(of(testList([])));
    TestBed.configureTestingModule({
      providers: [
        { provide: ExpensesApiService, useValue: api },
        { provide: AuthStore, useValue: { user } },
      ],
    });
    store = TestBed.inject(ExpensesStore);
  });

  it('load asks for the first page without filters and keeps the totals', () => {
    const items = [testExpense({ amount: 12.5 }), testExpense({ amount: 7.5 })];
    api.list.and.returnValue(of(testList(items, { totalCount: 42, totalAmount: 1234.56 })));

    store.load();

    expect(api.list).toHaveBeenCalledOnceWith({
      page: 1,
      pageSize: 20,
      category: null,
      from: null,
      to: null,
    });
    expect(store.items()).toEqual(items);
    expect(store.totalCount()).toBe(42);
    expect(store.totalAmount()).toBe(1234.56);
    expect(store.loading()).toBeFalse();
  });

  it('is loading until the response arrives', () => {
    const response = new Subject<ExpenseList>();
    api.list.and.returnValue(response);

    store.load();
    expect(store.loading()).toBeTrue();

    response.next(testList([testExpense()]));
    expect(store.loading()).toBeFalse();
  });

  it('setFilters goes back to page 1 and sends the filters', () => {
    store.setPage(3, 20);

    store.setFilters({ category: 'Food', from: '2026-10-01', to: '2026-10-31' });

    expect(api.list).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      category: 'Food',
      from: '2026-10-01',
      to: '2026-10-31',
    });
    expect(store.page()).toBe(1);
    expect(store.filters().category).toBe('Food');
  });

  it('setPage keeps the filters and changes page and size', () => {
    store.setFilters({ category: 'Food', from: null, to: null });

    store.setPage(2, 50);

    expect(api.list.calls.mostRecent().args[0]).toEqual({
      page: 2,
      pageSize: 50,
      category: 'Food',
      from: null,
      to: null,
    });
  });

  it('ignores a slow response that arrives after a newer one', () => {
    const slow = new Subject<ExpenseList>();
    const newer = testExpense({ description: 'Newer' });
    api.list.and.returnValues(slow, of(testList([newer])));

    store.setFilters({ category: 'Food', from: null, to: null });
    store.setFilters({ category: 'Health', from: null, to: null });
    slow.next(testList([testExpense({ description: 'Stale' })]));

    expect(store.items().map((e) => e.description)).toEqual(['Newer']);
    expect(slow.observed).toBeFalse();
  });

  it('records a failed load and clears it on the next successful one', () => {
    api.list.and.returnValue(throwError(() => new Error('500')));
    store.load();
    expect(store.error()).toBeTrue();
    expect(store.loading()).toBeFalse();

    api.list.and.returnValue(of(testList([testExpense()])));
    store.load();

    expect(store.error()).toBeFalse();
    expect(store.items().length).toBe(1);
  });

  it('keeps working after a failed load (the pipeline does not die)', () => {
    api.list.and.returnValues(
      throwError(() => new Error('500')),
      of(testList([testExpense()])),
    );

    store.load();
    store.load();

    expect(api.list).toHaveBeenCalledTimes(2);
    expect(store.items().length).toBe(1);
  });

  it('create saves and then reloads the current page', () => {
    const created = testExpense();
    api.create.and.returnValue(of(created));
    const input = { description: 'Lunch', amount: 12.5, category: 'Food', date: '2026-10-22' };

    store.create(input).subscribe();

    expect(api.create).toHaveBeenCalledOnceWith(input);
    expect(api.list).toHaveBeenCalledTimes(1);
  });

  it('update saves and then reloads the current page', () => {
    api.update.and.returnValue(of(testExpense()));
    const input = { description: 'Dinner', amount: 40, category: 'Food', date: '2026-10-22' };

    store.update('id-1', input).subscribe();

    expect(api.update).toHaveBeenCalledOnceWith('id-1', input);
    expect(api.list).toHaveBeenCalledTimes(1);
  });

  it('a failed create does not reload and passes the error to the form', () => {
    api.create.and.returnValue(throwError(() => new Error('400')));
    let failed = false;

    store
      .create({ description: 'x', amount: 0, category: 'Food', date: '2026-10-22' })
      .subscribe({ error: () => (failed = true) });

    expect(failed).toBeTrue();
    expect(api.list).not.toHaveBeenCalled();
  });

  it('removing the last expense of a page goes back one page', () => {
    api.list.and.returnValue(of(testList([testExpense()], { page: 2, totalCount: 21 })));
    store.setPage(2, 20);
    api.remove.and.returnValue(of(undefined));

    store.remove(store.items()[0].id).subscribe();

    expect(store.page()).toBe(1);
    expect(api.list.calls.mostRecent().args[0].page).toBe(1);
  });

  it('forgets the list when the signed-in user changes', () => {
    api.list.and.returnValue(of(testList([testExpense()])));
    store.setFilters({ category: 'Food', from: null, to: null });
    TestBed.tick();

    user.set({ id: 'other', email: 'other@ballastlane.com', displayName: 'Second User' });
    TestBed.tick();

    expect(store.items()).toEqual([]);
    expect(store.totalCount()).toBe(0);
    expect(store.filters()).toEqual({ category: null, from: null, to: null });
    expect(store.page()).toBe(1);
  });
});

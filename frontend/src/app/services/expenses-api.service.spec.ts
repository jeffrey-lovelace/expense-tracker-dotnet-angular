import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_CONFIG } from '../api-config';
import { testApiConfig } from '../testing/test-data';
import { testExpense } from '../testing/expense-data';
import { CategoriesApiService } from './categories-api.service';
import { ExpensesApiService } from './expenses-api.service';

describe('ExpensesApiService', () => {
  let api: ExpensesApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: testApiConfig },
      ],
    });
    api = TestBed.inject(ExpensesApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list sends paging and only the filters that are set', () => {
    api.list({ page: 2, pageSize: 20, category: 'Food', from: '2026-10-01', to: null }).subscribe();

    const request = http.expectOne((r) => r.url === 'http://expenses.test/api/expenses');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().sort()).toEqual(['category', 'from', 'page', 'pageSize']);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('category')).toBe('Food');
    expect(request.request.params.get('from')).toBe('2026-10-01');
    request.flush({ items: [], page: 2, pageSize: 20, totalCount: 0, totalAmount: 0 });
  });

  it('get, create, update and remove use the REST verbs of the contract', () => {
    const expense = testExpense();
    const input = { description: 'Lunch', amount: 12.5, category: 'Food', date: '2026-10-22' };

    api.get(expense.id).subscribe();
    api.create(input).subscribe();
    api.update(expense.id, input).subscribe();
    api.remove(expense.id).subscribe();

    const url = `http://expenses.test/api/expenses/${expense.id}`;
    expect(http.expectOne({ method: 'GET', url }).request).toBeTruthy();
    const created = http.expectOne({ method: 'POST', url: 'http://expenses.test/api/expenses' });
    expect(created.request.body).toEqual(input);
    const updated = http.expectOne({ method: 'PUT', url });
    expect(updated.request.body).toEqual(input);
    http.expectOne({ method: 'DELETE', url });
  });
});

describe('CategoriesApiService', () => {
  it('gets the public category list', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: testApiConfig },
      ],
    });
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(CategoriesApiService).list().subscribe();

    http.expectOne({ method: 'GET', url: 'http://expenses.test/api/categories' }).flush([]);
    http.verify();
  });
});

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../api-config';
import { Expense, ExpenseInput, ExpenseList, ExpenseListQuery } from '../models/expense.models';

/** Expenses.Api over HTTP. The token is added by the auth interceptor, never here. */
@Injectable({ providedIn: 'root' })
export class ExpensesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_CONFIG).expensesApiUrl}/expenses`;

  list(query: ExpenseListQuery): Observable<ExpenseList> {
    // Unset filters are left out, so the API applies no condition for them.
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    for (const key of ['category', 'from', 'to'] as const) {
      const value = query[key];
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<ExpenseList>(this.baseUrl, { params });
  }

  get(id: string): Observable<Expense> {
    return this.http.get<Expense>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  create(input: ExpenseInput): Observable<Expense> {
    return this.http.post<Expense>(this.baseUrl, input);
  }

  update(id: string, input: ExpenseInput): Observable<Expense> {
    return this.http.put<Expense>(`${this.baseUrl}/${encodeURIComponent(id)}`, input);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }
}

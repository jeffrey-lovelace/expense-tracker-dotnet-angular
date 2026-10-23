import { HttpClient } from '@angular/common/http';
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
    throw new Error(`Not implemented: ${this.baseUrl} ${query.page} ${this.http}`);
  }

  get(id: string): Observable<Expense> {
    throw new Error(`Not implemented: ${id}`);
  }

  create(input: ExpenseInput): Observable<Expense> {
    throw new Error(`Not implemented: ${input.description}`);
  }

  update(id: string, input: ExpenseInput): Observable<Expense> {
    throw new Error(`Not implemented: ${id} ${input.description}`);
  }

  remove(id: string): Observable<void> {
    throw new Error(`Not implemented: ${id}`);
  }
}

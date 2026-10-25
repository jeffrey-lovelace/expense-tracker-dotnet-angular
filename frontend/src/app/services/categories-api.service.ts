import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../api-config';
import { Category } from '../models/expense.models';

/** GET /api/categories (public). */
@Injectable({ providedIn: 'root' })
export class CategoriesApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_CONFIG).expensesApiUrl}/categories`;

  list(): Observable<Category[]> {
    return this.http.get<Category[]>(this.url);
  }
}

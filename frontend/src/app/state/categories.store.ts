import { inject, Injectable, Signal, signal } from '@angular/core';

import { Category } from '../models/expense.models';
import { CategoriesApiService } from '../services/categories-api.service';

/** The fixed category list, fetched once per session and shared by filters, form and labels. */
@Injectable({ providedIn: 'root' })
export class CategoriesStore {
  private readonly api = inject(CategoriesApiService);
  private readonly list = signal<readonly Category[]>([]);

  readonly categories: Signal<readonly Category[]> = this.list.asReadonly();

  load(): void {
    throw new Error(`Not implemented ${this.api}`);
  }
}

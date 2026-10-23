import { inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { Category } from '../models/expense.models';
import { CategoriesApiService } from '../services/categories-api.service';

/** The fixed category list, fetched once per session and shared by filters, form and labels. */
@Injectable({ providedIn: 'root' })
export class CategoriesStore {
  private readonly api = inject(CategoriesApiService);
  private readonly list = signal<readonly Category[]>([]);
  private loading = false;

  readonly categories: Signal<readonly Category[]> = this.list.asReadonly();

  /** Fetches the list unless it is already loaded or on its way. After a failure the next call tries again. */
  load(): void {
    if (this.loading || this.list().length > 0) {
      return;
    }

    this.loading = true;
    this.api
      .list()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (categories) => this.list.set(categories),
        // The error interceptor already told the user; the form shows an empty list until a retry.
        error: () => undefined,
      });
  }
}

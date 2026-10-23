import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged, filter, map } from 'rxjs';

import { fromIsoDate, todayIso, toIsoDate } from '../../dates/iso-date';
import { Category, ExpenseFilters, noFilters } from '../../models/expense.models';

/**
 * Category and date-range filters. Presentational: receives the current filters and emits new ones. RxJS shapes
 * the stream: while a range has only its start picked nothing is emitted, debounceTime merges the two picker
 * events into one, and distinctUntilChanged drops repeats, so each real change is exactly one request.
 */
@Component({
  selector: 'app-expense-filters',
  imports: [
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './expense-filters.html',
  styleUrl: './expense-filters.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseFiltersBar {
  readonly categories = input.required<readonly Category[]>();
  readonly filters = input.required<ExpenseFilters>();
  readonly filtersChange = output<ExpenseFilters>();

  protected readonly today = fromIsoDate(todayIso());

  protected readonly form = new FormGroup({
    category: new FormControl<string | null>(null),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null),
  });

  constructor() {
    // Mirror the store's filters (initial load, "clear filters" from the empty state) without echoing them back.
    effect(() => {
      const { category, from, to } = this.filters();
      this.form.setValue(
        { category, from: from ? fromIsoDate(from) : null, to: to ? fromIsoDate(to) : null },
        { emitEvent: false },
      );
    });

    this.form.valueChanges
      .pipe(
        map(() => this.form.getRawValue()),
        filter(({ from, to }) => (from === null) === (to === null)),
        debounceTime(150),
        map(({ category, from, to }): ExpenseFilters => ({
          category: category || null,
          from: from ? toIsoDate(from) : null,
          to: to ? toIsoDate(to) : null,
        })),
        distinctUntilChanged(
          (a, b) => a.category === b.category && a.from === b.from && a.to === b.to,
        ),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((filters) => this.filtersChange.emit(filters));
  }

  protected readonly hasFilters = computed(() => {
    const { category, from, to } = this.filters();
    return category !== null || from !== null || to !== null;
  });

  protected clear(): void {
    this.filtersChange.emit(noFilters);
  }
}

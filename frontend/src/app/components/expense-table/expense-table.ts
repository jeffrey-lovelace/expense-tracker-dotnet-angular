import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { Category, Expense } from '../../models/expense.models';
import { CategoryLabelPipe } from '../../pipes/category-label.pipe';
import { LedgerDatePipe } from '../../pipes/ledger-date.pipe';
import { MoneyPipe } from '../../pipes/money.pipe';
import { Skeleton } from '../skeleton/skeleton';

/**
 * The ledger table for 600px and wider (AGENTS.md 12.4): hairline rows, mono date, category marker, right-aligned
 * mono amount; row actions appear on hover or focus (always on touch). Presentational.
 */
@Component({
  selector: 'app-expense-table',
  imports: [
    CategoryLabelPipe,
    LedgerDatePipe,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MoneyPipe,
    Skeleton,
  ],
  templateUrl: './expense-table.html',
  styleUrl: './expense-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseTable {
  readonly expenses = input.required<readonly Expense[]>();
  readonly categories = input.required<readonly Category[]>();
  readonly loading = input(false);

  readonly edit = output<Expense>();
  readonly remove = output<Expense>();

  /** Eight placeholder rows with the real column widths. */
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];
}

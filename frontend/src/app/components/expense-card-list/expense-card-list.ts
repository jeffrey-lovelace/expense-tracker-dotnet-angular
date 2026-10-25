import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { Category, Expense } from '../../models/expense.models';
import { CategoryLabelPipe } from '../../pipes/category-label.pipe';
import { LedgerDatePipe } from '../../pipes/ledger-date.pipe';
import { MoneyPipe } from '../../pipes/money.pipe';
import { Skeleton } from '../skeleton/skeleton';

/** The same ledger as a list of cards, for screens narrower than 600px. Presentational. */
@Component({
  selector: 'app-expense-card-list',
  imports: [CategoryLabelPipe, LedgerDatePipe, MatButtonModule, MatIconModule, MoneyPipe, Skeleton],
  templateUrl: './expense-card-list.html',
  styleUrl: './expense-card-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseCardList {
  readonly expenses = input.required<readonly Expense[]>();
  readonly categories = input.required<readonly Category[]>();
  readonly loading = input(false);

  readonly edit = output<Expense>();
  readonly remove = output<Expense>();

  protected readonly skeletonCards = [1, 2, 3, 4, 5, 6];
}

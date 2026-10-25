import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { MoneyPipe } from '../../pipes/money.pipe';
import { Skeleton } from '../skeleton/skeleton';

/** The period header: the total for the filtered period in display size, the range as an overline (AGENTS.md 12.4). */
@Component({
  selector: 'app-expense-summary',
  imports: [MoneyPipe, Skeleton],
  templateUrl: './expense-summary.html',
  styleUrl: './expense-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseSummary {
  readonly period = input.required<string>();
  readonly total = input.required<number>();
  readonly count = input.required<number>();
  readonly loading = input(false);
}

import { BreakpointObserver } from '@angular/cdk/layout';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  OnInit,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthStore } from '../../auth/auth.store';
import { AppShell } from '../../components/app-shell/app-shell';
import { EmptyState } from '../../components/empty-state/empty-state';
import { ExpenseCardList } from '../../components/expense-card-list/expense-card-list';
import { ExpenseFiltersBar } from '../../components/expense-filters/expense-filters';
import { ExpenseSummary } from '../../components/expense-summary/expense-summary';
import { ExpenseTable } from '../../components/expense-table/expense-table';
import { Expense, ExpenseFilters, ExpenseInput, noFilters } from '../../models/expense.models';
import { LedgerDatePipe } from '../../pipes/ledger-date.pipe';
import { NotificationService } from '../../services/notification.service';
import { CategoriesStore } from '../../state/categories.store';
import { ExpensesStore } from '../../state/expenses.store';

/** The ledger. A page: talks to the stores; the components below it only get inputs and emit outputs. */
@Component({
  selector: 'app-expenses-page',
  imports: [
    AppShell,
    EmptyState,
    ExpenseCardList,
    ExpenseFiltersBar,
    ExpenseSummary,
    ExpenseTable,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
  ],
  templateUrl: './expenses-page.html',
  styleUrl: './expenses-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpensesPage implements OnInit {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly injector = inject(Injector);
  protected readonly expenses = inject(ExpensesStore);
  protected readonly categories = inject(CategoriesStore);

  private readonly ledgerDate = new LedgerDatePipe();

  /** Table at 600px and wider, cards below (AGENTS.md 11.2). */
  protected readonly wide = toSignal(
    inject(BreakpointObserver)
      .observe('(min-width: 600px)')
      .pipe(map((state) => state.matches)),
    { initialValue: true },
  );

  protected readonly userName = computed(() => this.auth.user()?.displayName ?? '');

  protected readonly hasFilters = computed(() => {
    const { category, from, to } = this.expenses.filters();
    return category !== null || from !== null || to !== null;
  });

  /** The overline above the total: the date range and category the total covers. */
  protected readonly period = computed(() => {
    const { category, from, to } = this.expenses.filters();
    const range =
      from && to
        ? `${this.ledgerDate.transform(from, true)} to ${this.ledgerDate.transform(to, true)}`
        : 'All dates';
    return category ? `${range}, ${category}` : range;
  });

  constructor() {
    // Leaving the page (sign-out, or the error interceptor sending an expired session to /login) must not leave
    // a dialog open over the next page: Material only closes dialogs by itself on back/forward navigation.
    inject(DestroyRef).onDestroy(() => this.dialog.closeAll());
  }

  ngOnInit(): void {
    this.categories.load();
    this.expenses.load();
  }

  protected applyFilters(filters: ExpenseFilters): void {
    this.expenses.setFilters(filters);
  }

  protected clearFilters(): void {
    this.expenses.setFilters(noFilters);
  }

  protected changePage(event: PageEvent): void {
    this.expenses.setPage(event.pageIndex + 1, event.pageSize);
  }

  protected add(): void {
    void this.openForm(undefined, (input) => this.expenses.create(input));
  }

  protected edit(expense: Expense): void {
    void this.openForm(expense, (input) => this.expenses.update(expense.id, input));
  }

  protected async remove(expense: Expense): Promise<void> {
    // Loaded on demand: the dialog's code is a separate chunk.
    const { ConfirmDialog } = await import('../../dialogs/confirm-dialog/confirm-dialog');
    const confirmed = this.dialog.open(ConfirmDialog, {
      data: {
        title: 'Delete this expense?',
        message: `"${expense.description}" will be removed from your ledger. This cannot be undone.`,
        confirmLabel: 'Delete',
      },
      width: '400px',
      maxWidth: 'calc(100vw - 32px)',
    });

    confirmed.afterClosed().subscribe((yes) => {
      if (yes !== true) {
        return;
      }

      this.expenses.remove(expense.id).subscribe({
        next: () => this.notifications.info('Expense deleted'),
        error: (error: unknown) => {
          // Already gone (deleted in another tab): refresh so the row disappears.
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.notifications.info('That expense was already deleted.');
            this.expenses.load();
          }
        },
      });
    });
  }

  protected signOut(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  private async openForm(
    expense: Expense | undefined,
    save: (input: ExpenseInput) => ReturnType<ExpensesStore['create']>,
  ): Promise<void> {
    // Loaded on demand: the form dialog (and its date picker and select) is a separate chunk.
    const { ExpenseFormDialog } =
      await import('../../dialogs/expense-form-dialog/expense-form-dialog');
    const ref = this.dialog.open(ExpenseFormDialog, {
      data: { expense, categories: this.categories.categories(), save },
      // The route's injector, so the dialog finds the date adapter provided on the expenses route.
      injector: this.injector,
      ...this.dialogSize(),
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved === true) {
        this.notifications.info('Expense saved');
      }
    });
  }

  // Full screen on small viewports (AGENTS.md 11.2).
  private dialogSize(): MatDialogConfig {
    return this.wide()
      ? { width: '520px', maxWidth: 'calc(100vw - 32px)' }
      : {
          width: '100vw',
          maxWidth: '100vw',
          height: '100dvh',
          maxHeight: '100dvh',
          panelClass: 'et-dialog-fullscreen',
        };
  }
}

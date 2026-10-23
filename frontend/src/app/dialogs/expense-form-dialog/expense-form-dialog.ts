import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, Observable } from 'rxjs';

import { fromIsoDate, todayIso, toIsoDate } from '../../dates/iso-date';
import { maxDecimals, notAfter } from '../../forms/expense.validators';
import { applyServerErrors } from '../../forms/server-errors';
import { Category, Expense, ExpenseInput } from '../../models/expense.models';

export interface ExpenseFormData {
  /** The expense to edit, or undefined to create one. */
  readonly expense?: Expense;
  readonly categories: readonly Category[];
  /** Saves the input (create or update). Kept by the dialog so a 400 can be shown next to the fields. */
  readonly save: (input: ExpenseInput) => Observable<unknown>;
}

/**
 * Create and edit. The dialog runs the save itself and closes with `true` only on success; on 400 the API's field
 * errors are placed on the matching controls and the dialog stays open with what the user typed.
 */
@Component({
  selector: 'app-expense-form-dialog',
  imports: [
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './expense-form-dialog.html',
  styleUrl: './expense-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseFormDialog {
  private readonly dialogRef = inject<MatDialogRef<ExpenseFormDialog, boolean>>(MatDialogRef);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly data = inject<ExpenseFormData>(MAT_DIALOG_DATA);

  protected readonly editing = this.data.expense !== undefined;
  protected readonly minDate = new Date(2000, 0, 1);
  protected readonly maxDate = fromIsoDate(todayIso());

  // Same limits as Expenses.Domain, for instant feedback. The API stays the authority.
  protected readonly form = new FormGroup({
    description: new FormControl(this.data.expense?.description ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(200)],
    }),
    amount: new FormControl<number | null>(this.data.expense?.amount ?? null, [
      Validators.required,
      Validators.min(0.01),
      Validators.max(1_000_000),
      maxDecimals(2),
    ]),
    category: new FormControl(this.data.expense?.category ?? '', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    date: new FormControl<Date | null>(
      this.data.expense ? fromIsoDate(this.data.expense.date) : fromIsoDate(todayIso()),
      [Validators.required, notAfter(() => todayIso())],
    ),
  });

  protected readonly pending = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { description, amount, category, date } = this.form.getRawValue();
    const input: ExpenseInput = {
      description: description.trim(),
      amount: amount!,
      category,
      date: toIsoDate(date!),
    };

    this.pending.set(true);
    this.formError.set(null);
    this.data
      .save(input)
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (error: unknown) => this.showError(error),
      });
  }

  private showError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      return;
    }

    if (error.status === 400) {
      const rest = applyServerErrors(this.form, error);
      this.formError.set(rest.length > 0 ? rest.join(' ') : null);
    } else if (error.status === 404) {
      this.formError.set('This expense no longer exists. Close the dialog to refresh the list.');
    }
  }
}

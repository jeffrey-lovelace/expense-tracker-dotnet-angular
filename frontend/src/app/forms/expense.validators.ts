import { ValidatorFn } from '@angular/forms';

/**
 * The expense form's copies of two backend rules (Expenses.Domain Expense.Create), for instant feedback only.
 * Required, length and range use the built-in validators.
 */

/** { maxDecimals: { max } } when the number has more decimal places than allowed (12.345 with max 2). */
export function maxDecimals(max: number): ValidatorFn {
  throw new Error(`Not implemented: ${max}`);
}

/** { notAfter: { max } } when the date (a Date from the picker) is later than `latest()` ("yyyy-MM-dd"). */
export function notAfter(latest: () => string): ValidatorFn {
  throw new Error(`Not implemented: ${latest}`);
}

import { ValidationErrors, ValidatorFn } from '@angular/forms';

import { toIsoDate } from '../dates/iso-date';

/**
 * The expense form's copies of two backend rules (Expenses.Domain Expense.Create), for instant feedback only.
 * Required, length and range use the built-in validators.
 */

/** { maxDecimals: { max } } when the number has more decimal places than allowed (12.345 with max 2). */
export function maxDecimals(max: number): ValidatorFn {
  // Checked on the decimal text, not with arithmetic, so 0.1 + 0.2 style float noise cannot fool it.
  const pattern = new RegExp(`^-?\\d+(\\.\\d{1,${max}})?$`);
  return (control): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return pattern.test(String(value)) ? null : { maxDecimals: { max } };
  };
}

/** { notAfter: { max } } when the date (a Date from the picker) is later than `latest()` ("yyyy-MM-dd"). */
export function notAfter(latest: () => string): ValidatorFn {
  return (control): ValidationErrors | null => {
    const value: unknown = control.value;
    if (!(value instanceof Date)) {
      return null;
    }

    const max = latest();
    // "yyyy-MM-dd" strings sort like dates.
    return toIsoDate(value) > max ? { notAfter: { max } } : null;
  };
}

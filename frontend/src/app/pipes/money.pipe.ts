import { Pipe, PipeTransform } from '@angular/core';

// Created once: Intl formatters are expensive to build and safe to share.
const formatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/**
 * 1234.5 -> "1,234.50": grouping and always two decimals. No currency symbol, because the data model has no currency;
 * the template sets the monospaced, tabular-figure font so columns line up.
 */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value === null || value === undefined ? '' : formatter.format(value);
  }
}

import { Pipe, PipeTransform } from '@angular/core';

/**
 * 1234.5 -> "1,234.50": grouping and always two decimals. No currency symbol, because the data model has no currency;
 * the template sets the monospaced, tabular-figure font so columns line up.
 */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    throw new Error(`Not implemented: ${value}`);
  }
}

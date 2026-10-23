import { Pipe, PipeTransform } from '@angular/core';

import { IsoDate } from '../models/expense.models';

const months = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];

/** "2026-10-22" -> "OCT 22" for the ledger's date column. Reads the parts directly: no Date, no time zone. */
@Pipe({ name: 'ledgerDate' })
export class LedgerDatePipe implements PipeTransform {
  transform(value: IsoDate | null | undefined, withYear = false): string {
    const match = value ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(value) : null;
    if (match === null) {
      return '';
    }

    const [, year, month, day] = match;
    const label = `${months[Number(month) - 1]} ${day}`;
    return withYear ? `${label}, ${year}` : label;
  }
}

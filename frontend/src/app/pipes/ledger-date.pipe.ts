import { Pipe, PipeTransform } from '@angular/core';

import { IsoDate } from '../models/expense.models';

/** "2026-10-22" -> "OCT 22" for the ledger's date column. Reads the parts directly: no Date, no time zone. */
@Pipe({ name: 'ledgerDate' })
export class LedgerDatePipe implements PipeTransform {
  transform(value: IsoDate | null | undefined, withYear = false): string {
    throw new Error(`Not implemented: ${value} ${withYear}`);
  }
}

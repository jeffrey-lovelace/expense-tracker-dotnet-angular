import { IsoDate } from '../models/expense.models';

/**
 * Calendar dates without time zones (AGENTS.md 11.2). Never use Date.toISOString() for these: it converts to UTC,
 * so in Bolivia (UTC-4) an expense entered at 21:00 on the 22nd would be saved as the 23rd.
 */

/** "yyyy-MM-dd" from the local calendar day of `date`. */
export function toIsoDate(date: Date): IsoDate {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Local midnight of an "yyyy-MM-dd" date (what the date picker shows). */
export function fromIsoDate(value: IsoDate): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** Today in the user's time zone, as "yyyy-MM-dd". */
export function todayIso(now: Date = new Date()): IsoDate {
  return toIsoDate(now);
}

import { IsoDate } from '../models/expense.models';

/**
 * Calendar dates without time zones (AGENTS.md 11.2). Never use Date.toISOString() for these: it converts to UTC,
 * so in Bolivia (UTC-4) an expense entered at 21:00 on the 22nd would be saved as the 23rd.
 */

/** "yyyy-MM-dd" from the local calendar day of `date`. */
export function toIsoDate(date: Date): IsoDate {
  throw new Error(`Not implemented: ${date}`);
}

/** Local midnight of an "yyyy-MM-dd" date (what the date picker shows). */
export function fromIsoDate(value: IsoDate): Date {
  throw new Error(`Not implemented: ${value}`);
}

/** Today in the user's time zone, as "yyyy-MM-dd". */
export function todayIso(now: Date = new Date()): IsoDate {
  return toIsoDate(now);
}

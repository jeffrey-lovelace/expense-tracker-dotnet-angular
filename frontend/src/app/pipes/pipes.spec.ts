import { CategoryLabelPipe } from './category-label.pipe';
import { LedgerDatePipe } from './ledger-date.pipe';
import { MoneyPipe } from './money.pipe';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('groups thousands and always shows two decimals', () => {
    expect(pipe.transform(1234.5)).toBe('1,234.50');
    expect(pipe.transform(0.1)).toBe('0.10');
    expect(pipe.transform(1000000)).toBe('1,000,000.00');
  });

  it('rounds floating point noise to cents', () => {
    expect(pipe.transform(0.1 + 0.2)).toBe('0.30');
  });

  it('shows nothing for a missing value', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});

describe('LedgerDatePipe', () => {
  const pipe = new LedgerDatePipe();

  it('formats as short month and day', () => {
    expect(pipe.transform('2026-10-22')).toBe('OCT 22');
    expect(pipe.transform('2026-01-05')).toBe('JAN 05');
  });

  it('can add the year', () => {
    expect(pipe.transform('2026-10-22', true)).toBe('OCT 22, 2026');
  });

  it('never shifts the day, whatever the time zone', () => {
    // A Date-based formatter would read "2026-03-01" as UTC midnight and show FEB 28 west of Greenwich.
    expect(pipe.transform('2026-03-01')).toBe('MAR 01');
  });

  it('shows nothing for a missing or malformed value', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform('22/10/2026')).toBe('');
  });
});

describe('CategoryLabelPipe', () => {
  const pipe = new CategoryLabelPipe();
  const categories = [
    { value: 'Food', label: 'Food' },
    { value: 'Health', label: 'Health and care' },
  ];

  it('returns the label of a known category', () => {
    expect(pipe.transform('Health', categories)).toBe('Health and care');
  });

  it('falls back to the value', () => {
    expect(pipe.transform('Other', categories)).toBe('Other');
    expect(pipe.transform('Other', [])).toBe('Other');
  });
});

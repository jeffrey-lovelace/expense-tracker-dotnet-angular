import { fromIsoDate, todayIso, toIsoDate } from './iso-date';

describe('iso dates', () => {
  it('uses the local calendar day, not the UTC one', () => {
    // 21:00 local on Oct 22. In UTC-4 that instant is already Oct 23 in UTC; toISOString() would say "2026-10-23".
    const lateEvening = new Date(2026, 9, 22, 21, 0, 0);

    expect(toIsoDate(lateEvening)).toBe('2026-10-22');
  });

  it('pads months and days', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('reads an iso date as local midnight of that day', () => {
    const date = fromIsoDate('2026-02-28');

    expect(date.getFullYear()).toBe(2026);
    expect(date.getMonth()).toBe(1);
    expect(date.getDate()).toBe(28);
    expect(date.getHours()).toBe(0);
  });

  it('round-trips every day of a leap year', () => {
    for (
      let day = new Date(2028, 0, 1);
      day.getFullYear() === 2028;
      day.setDate(day.getDate() + 1)
    ) {
      const iso = toIsoDate(day);

      expect(toIsoDate(fromIsoDate(iso))).toBe(iso);
    }
  });

  it('todayIso is the local date of now', () => {
    expect(todayIso(new Date(2026, 9, 22, 23, 59))).toBe('2026-10-22');
  });
});

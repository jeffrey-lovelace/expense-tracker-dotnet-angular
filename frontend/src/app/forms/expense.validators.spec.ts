import { FormControl } from '@angular/forms';

import { maxDecimals, notAfter } from './expense.validators';

describe('maxDecimals', () => {
  const validator = maxDecimals(2);

  for (const ok of [12, 12.5, 12.55, 0.01, 1000000]) {
    it(`accepts ${ok}`, () => {
      expect(validator(new FormControl(ok))).toBeNull();
    });
  }

  for (const bad of [12.345, 0.001]) {
    it(`rejects ${bad}`, () => {
      expect(validator(new FormControl(bad))).toEqual({ maxDecimals: { max: 2 } });
    });
  }

  it('leaves an empty value to Validators.required', () => {
    expect(validator(new FormControl(null))).toBeNull();
  });
});

describe('notAfter', () => {
  const validator = notAfter(() => '2026-10-22');

  it('accepts today and earlier days', () => {
    expect(validator(new FormControl(new Date(2026, 9, 22, 23, 30)))).toBeNull();
    expect(validator(new FormControl(new Date(2026, 9, 1)))).toBeNull();
  });

  it('rejects a later day', () => {
    expect(validator(new FormControl(new Date(2026, 9, 23)))).toEqual({
      notAfter: { max: '2026-10-22' },
    });
  });

  it('leaves an empty value to Validators.required', () => {
    expect(validator(new FormControl(null))).toBeNull();
  });
});

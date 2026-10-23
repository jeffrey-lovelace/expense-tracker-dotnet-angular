import { FormControl } from '@angular/forms';

import { passwordStrength } from './password-strength.validator';

describe('passwordStrength', () => {
  function check(value: string): Record<string, unknown> | null {
    return passwordStrength(new FormControl(value));
  }

  it('accepts a password with upper, lower, digit and symbol', () => {
    expect(check('Demo123!')).toBeNull();
  });

  it('counts a space as a symbol, like the backend', () => {
    expect(check('Abcdef1 ')).toBeNull();
  });

  it('leaves an empty value to Validators.required', () => {
    expect(check('')).toBeNull();
  });

  it('reports every missing requirement', () => {
    expect(check('abc')).toEqual({ uppercase: true, digit: true, symbol: true });
    expect(check('ABC1!')).toEqual({ lowercase: true });
  });
});

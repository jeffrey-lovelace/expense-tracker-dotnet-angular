import { ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * The register form's copy of the backend password rules (Identity.Domain User.Create), for instant feedback only.
 * The API stays the authority. Error keys: uppercase, lowercase, digit, symbol (length uses Validators.minLength).
 * Unicode-aware like the backend's char.IsUpper/IsLower: "Pässwörd1#" passes.
 */
export const passwordStrength: ValidatorFn = (control): ValidationErrors | null => {
  const value: unknown = control.value;
  if (typeof value !== 'string' || value.length === 0) {
    return null;
  }

  const errors: ValidationErrors = {};
  if (!/\p{Lu}/u.test(value)) {
    errors['uppercase'] = true;
  }
  if (!/\p{Ll}/u.test(value)) {
    errors['lowercase'] = true;
  }
  if (!/\p{Nd}/u.test(value)) {
    errors['digit'] = true;
  }
  if (!/[^\p{L}\p{Nd}]/u.test(value)) {
    errors['symbol'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
};

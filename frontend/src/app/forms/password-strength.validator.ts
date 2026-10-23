import { ValidatorFn } from '@angular/forms';

/**
 * The register form's copy of the backend password rules (Identity.Domain User.Create), for instant feedback only.
 * The API stays the authority. Error keys: uppercase, lowercase, digit, symbol (length uses Validators.minLength).
 */
export const passwordStrength: ValidatorFn = () => {
  throw new Error('Not implemented');
};

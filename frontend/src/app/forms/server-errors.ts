import { FormGroup } from '@angular/forms';

import { problemOf } from '../models/problem-details';

/**
 * Puts the API's field errors (ProblemDetails.errors, keyed by camelCase field name) on the matching form controls
 * as { server: message }. Returns the messages that have no matching control, to show above the form.
 */
export function applyServerErrors(form: FormGroup, error: unknown): string[] {
  const errors = problemOf(error).errors;
  if (errors === undefined) {
    return [];
  }

  const unmatched: string[] = [];
  for (const [field, messages] of Object.entries(errors)) {
    const control = field === '' ? null : form.get(field);
    if (control === null) {
      unmatched.push(...messages);
      continue;
    }

    control.setErrors({ server: messages[0] });
    control.markAsTouched();
  }

  return unmatched;
}

import { FormGroup } from '@angular/forms';

/**
 * Puts the API's field errors (ProblemDetails.errors, keyed by camelCase field name) on the matching form controls
 * as { server: message }. Returns the messages that have no matching control, to show above the form.
 */
export function applyServerErrors(form: FormGroup, error: unknown): string[] {
  throw new Error(`Not implemented: ${form.status} ${String(error)}`);
}

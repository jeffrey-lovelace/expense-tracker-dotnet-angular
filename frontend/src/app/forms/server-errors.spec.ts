import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';

import { applyServerErrors } from './server-errors';

describe('applyServerErrors', () => {
  function form(): FormGroup {
    return new FormGroup({
      email: new FormControl('x'),
      displayName: new FormControl('x'),
      password: new FormControl('x'),
    });
  }

  function badRequest(errors: Record<string, string[]>): HttpErrorResponse {
    return new HttpErrorResponse({ status: 400, error: { status: 400, errors } });
  }

  it('puts each field error on the control with the same name', () => {
    const group = form();

    const rest = applyServerErrors(
      group,
      badRequest({ email: ['Enter a valid email address.'], displayName: ['Name is too short.'] }),
    );

    expect(group.get('email')?.errors).toEqual({ server: 'Enter a valid email address.' });
    expect(group.get('displayName')?.errors).toEqual({ server: 'Name is too short.' });
    expect(group.get('password')?.errors).toBeNull();
    expect(rest).toEqual([]);
  });

  it('shows the first message when a field has several, and marks the control touched', () => {
    const group = form();

    applyServerErrors(group, badRequest({ password: ['Too short.', 'Needs a digit.'] }));

    expect(group.get('password')?.errors).toEqual({ server: 'Too short.' });
    expect(group.get('password')?.touched).toBeTrue();
  });

  it('returns the messages that have no matching control', () => {
    const rest = applyServerErrors(
      form(),
      badRequest({ '': ['Request is malformed.'], other: ['Unknown.'] }),
    );

    expect(rest).toEqual(['Request is malformed.', 'Unknown.']);
  });

  it('ignores errors that are not validation problems', () => {
    const group = form();

    const rest = applyServerErrors(group, new HttpErrorResponse({ status: 500, error: 'boom' }));

    expect(rest).toEqual([]);
    expect(group.valid).toBeTrue();
  });
});

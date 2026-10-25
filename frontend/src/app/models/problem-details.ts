import { HttpErrorResponse } from '@angular/common/http';

/** RFC 7807 body that both APIs return for every error. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  /** Machine-readable error code, for example "user.email.taken". */
  readonly code?: string;
  /** Validation errors keyed by camelCase field name. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** The ProblemDetails inside an HTTP error, or an empty object when the body is something else. */
export function problemOf(error: unknown): ProblemDetails {
  if (
    error instanceof HttpErrorResponse &&
    error.error !== null &&
    typeof error.error === 'object'
  ) {
    return error.error as ProblemDetails;
  }

  return {};
}

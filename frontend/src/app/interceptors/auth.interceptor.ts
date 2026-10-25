import { HttpEvent, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../api-config';
import { AuthStore } from '../auth/auth.store';

/** Adds "Authorization: Bearer <token>" to requests for our two APIs only, never to other hosts. */
export const authInterceptor: HttpInterceptorFn = (
  request,
  next,
): Observable<HttpEvent<unknown>> => {
  const token = inject(AuthStore).token();
  const { identityApiUrl, expensesApiUrl } = inject(API_CONFIG);

  if (
    token === null ||
    (!isUnder(request.url, identityApiUrl) && !isUnder(request.url, expensesApiUrl))
  ) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

// "http://expenses.test/api" must not match "http://expenses.test/api-evil" or "http://expenses.test.evil.example".
function isUnder(url: string, baseUrl: string): boolean {
  return url === baseUrl || url.startsWith(`${baseUrl}/`) || url.startsWith(`${baseUrl}?`);
}

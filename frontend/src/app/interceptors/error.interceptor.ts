import { HttpErrorResponse, HttpEvent, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, throwError } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { NotificationService } from '../services/notification.service';

/**
 * Cross-cutting HTTP failures: an expired session (401 on a request that carried our token) signs the user out and
 * sends them to /login; no connection (0) and server errors (5xx) show a message. 400, 404, 409 and the 401 of a
 * failed login pass through untouched, so the page can show them next to the form. The error is always rethrown.
 */
export const errorInterceptor: HttpInterceptorFn = (
  request,
  next,
): Observable<HttpEvent<unknown>> => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && request.headers.has('Authorization')) {
          auth.logout();
          notifications.error('Your session has ended. Sign in again.');
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
        } else if (error.status === 0) {
          notifications.error('Cannot reach the server. Check your connection and try again.');
        } else if (error.status >= 500) {
          notifications.error('Something went wrong on our side. Try again in a moment.');
        }
      }

      return throwError(() => error);
    }),
  );
};

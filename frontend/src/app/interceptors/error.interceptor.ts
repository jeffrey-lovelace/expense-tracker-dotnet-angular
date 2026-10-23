import { HttpEvent, HttpInterceptorFn } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * Cross-cutting HTTP failures: an expired session (401 on a request that carried our token) signs the user out and
 * sends them to /login; no connection (0) and server errors (5xx) show a message. 400, 404, 409 and the 401 of a
 * failed login pass through untouched, so the page can show them next to the form. The error is always rethrown.
 */
export const errorInterceptor: HttpInterceptorFn = (): Observable<HttpEvent<unknown>> => {
  throw new Error('Not implemented');
};

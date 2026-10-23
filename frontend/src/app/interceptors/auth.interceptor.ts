import { HttpEvent, HttpInterceptorFn } from '@angular/common/http';
import { Observable } from 'rxjs';

/** Adds "Authorization: Bearer <token>" to requests for our two APIs only, never to other hosts. */
export const authInterceptor: HttpInterceptorFn = (): Observable<HttpEvent<unknown>> => {
  throw new Error('Not implemented');
};

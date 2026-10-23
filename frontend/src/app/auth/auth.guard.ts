import { CanActivateFn, UrlTree } from '@angular/router';

/** Private routes: signed-out users go to /login?returnUrl=... */
export const authGuard: CanActivateFn = (): boolean | UrlTree => {
  throw new Error('Not implemented');
};

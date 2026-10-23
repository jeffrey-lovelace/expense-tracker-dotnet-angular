import { CanActivateFn, UrlTree } from '@angular/router';

/** /login and /register: a signed-in user goes straight to their expenses. */
export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  throw new Error('Not implemented');
};

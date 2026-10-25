import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { AuthStore } from './auth.store';

/** /login and /register: a signed-in user goes straight to their expenses. */
export const guestGuard: CanActivateFn = (): boolean | UrlTree =>
  inject(AuthStore).isAuthenticated() ? inject(Router).createUrlTree(['/expenses']) : true;

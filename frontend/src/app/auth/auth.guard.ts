import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { AuthStore } from './auth.store';

/** Private routes: signed-out users go to /login?returnUrl=... */
export const authGuard: CanActivateFn = (_route, state): boolean | UrlTree => {
  if (inject(AuthStore).isAuthenticated()) {
    return true;
  }

  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

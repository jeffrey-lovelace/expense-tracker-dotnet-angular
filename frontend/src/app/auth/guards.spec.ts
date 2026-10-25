import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthStore } from './auth.store';
import { guestGuard } from './guest.guard';

describe('route guards', () => {
  const signedIn = signal(false);

  beforeEach(() => {
    signedIn.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthStore, useValue: { isAuthenticated: signedIn } },
      ],
    });
  });

  function run(guard: typeof authGuard, url = '/expenses?page=2'): boolean | UrlTree {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, state)) as
      boolean | UrlTree;
  }

  function serialize(result: boolean | UrlTree): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  describe('authGuard', () => {
    it('lets a signed-in user through', () => {
      signedIn.set(true);

      expect(run(authGuard)).toBeTrue();
    });

    it('sends a signed-out user to /login and remembers where they were going', () => {
      const result = run(authGuard, '/expenses?page=2');

      expect(result).toBeInstanceOf(UrlTree);
      expect(serialize(result)).toBe('/login?returnUrl=%2Fexpenses%3Fpage%3D2');
    });
  });

  describe('guestGuard', () => {
    it('lets a signed-out user see /login and /register', () => {
      expect(run(guestGuard, '/login')).toBeTrue();
    });

    it('sends a signed-in user to their expenses', () => {
      signedIn.set(true);

      expect(serialize(run(guestGuard, '/login'))).toBe('/expenses');
    });
  });
});

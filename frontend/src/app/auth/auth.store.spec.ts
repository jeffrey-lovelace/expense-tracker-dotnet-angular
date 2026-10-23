import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthApiService } from '../services/auth-api.service';
import { testSession } from '../testing/test-data';
import { AuthStore } from './auth.store';
import { TokenStorageService } from './token-storage.service';

describe('AuthStore', () => {
  let api: jasmine.SpyObj<AuthApiService>;
  let storage: jasmine.SpyObj<TokenStorageService>;

  function createStore(stored = testSession()): AuthStore {
    storage.read.and.returnValue(stored);
    return TestBed.inject(AuthStore);
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<AuthApiService>('AuthApiService', ['login', 'register', 'me']);
    storage = jasmine.createSpyObj<TokenStorageService>('TokenStorageService', [
      'read',
      'save',
      'clear',
    ]);
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthApiService, useValue: api },
        { provide: TokenStorageService, useValue: storage },
      ],
    });
  });

  it('starts signed in when a valid session is stored (page refresh)', () => {
    const session = testSession();

    const store = createStore(session);

    expect(store.isAuthenticated()).toBeTrue();
    expect(store.user()).toEqual(session.user);
    expect(store.token()).toBe(session.token);
  });

  it('starts signed out when nothing is stored', () => {
    storage.read.and.returnValue(null);

    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
    expect(store.token()).toBeNull();
  });

  it('login keeps the session in memory and in storage', () => {
    storage.read.and.returnValue(null);
    const store = TestBed.inject(AuthStore);
    const session = testSession();
    api.login.and.returnValue(of(session));
    let signedIn: unknown;

    store
      .login({ email: 'demo@ballastlane.com', password: 'Demo123!' })
      .subscribe((u) => (signedIn = u));

    expect(signedIn).toEqual(session.user);
    expect(store.isAuthenticated()).toBeTrue();
    expect(store.token()).toBe(session.token);
    expect(storage.save).toHaveBeenCalledOnceWith(session);
  });

  it('a failed login leaves the user signed out and passes the error on', () => {
    storage.read.and.returnValue(null);
    const store = TestBed.inject(AuthStore);
    api.login.and.returnValue(throwError(() => new Error('401')));
    let failed = false;

    store
      .login({ email: 'demo@ballastlane.com', password: 'nope' })
      .subscribe({ error: () => (failed = true) });

    expect(failed).toBeTrue();
    expect(store.isAuthenticated()).toBeFalse();
    expect(storage.save).not.toHaveBeenCalled();
  });

  it('register signs the new user in right away', () => {
    storage.read.and.returnValue(null);
    const store = TestBed.inject(AuthStore);
    const session = testSession();
    api.register.and.returnValue(of(session));

    store
      .register({ email: 'new@example.com', displayName: 'New User', password: 'Demo123!' })
      .subscribe();

    expect(store.isAuthenticated()).toBeTrue();
    expect(storage.save).toHaveBeenCalledOnceWith(session);
  });

  it('logout forgets the session everywhere', () => {
    const store = createStore();

    store.logout();

    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
    expect(store.token()).toBeNull();
    expect(storage.clear).toHaveBeenCalled();
  });

  it('a session past its expiry no longer counts as signed in', () => {
    jasmine.clock().install();
    try {
      const start = new Date('2026-10-23T12:00:00Z');
      jasmine.clock().mockDate(start);
      const store = createStore(testSession(start));
      expect(store.isAuthenticated()).toBeTrue();

      jasmine.clock().mockDate(new Date(start.getTime() + 61 * 60 * 1000));

      expect(store.isAuthenticated()).toBeFalse();
    } finally {
      jasmine.clock().uninstall();
    }
  });
});

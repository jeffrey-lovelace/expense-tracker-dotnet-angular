import { HttpClient, HttpHeaders, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { AuthStore } from '../auth/auth.store';
import { NotificationService } from '../services/notification.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  const url = 'http://expenses.test/api/expenses';
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: jasmine.SpyObj<AuthStore>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthStore>('AuthStore', ['logout']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'info',
      'error',
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate'], { url: '/expenses?page=2' });
    router.navigate.and.resolveTo(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: auth },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function fail(status: number, withToken: boolean): { rethrown: boolean } {
    const outcome = { rethrown: false };
    const headers = withToken ? new HttpHeaders({ Authorization: 'Bearer t' }) : new HttpHeaders();
    http.get(url, { headers }).subscribe({ error: () => (outcome.rethrown = true) });
    backend.expectOne(url).flush({ status }, { status, statusText: 'x' });
    return outcome;
  }

  it('signs out and goes to /login when a signed-in request gets 401 (expired session)', () => {
    const outcome = fail(401, true);

    expect(auth.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/expenses?page=2' },
    });
    expect(notifications.error).toHaveBeenCalled();
    expect(outcome.rethrown).toBeTrue();
  });

  it('leaves a 401 without a token (failed login) to the page', () => {
    const outcome = fail(401, false);

    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(notifications.error).not.toHaveBeenCalled();
    expect(outcome.rethrown).toBeTrue();
  });

  it('tells the user when the API cannot be reached', () => {
    const outcome = { rethrown: false };
    http.get(url).subscribe({ error: () => (outcome.rethrown = true) });

    backend.expectOne(url).error(new ProgressEvent('error'));

    expect(notifications.error).toHaveBeenCalledWith(jasmine.stringContaining('reach'));
    expect(outcome.rethrown).toBeTrue();
  });

  it('tells the user about server errors', () => {
    const outcome = fail(500, true);

    expect(notifications.error).toHaveBeenCalledWith(jasmine.stringContaining('went wrong'));
    expect(auth.logout).not.toHaveBeenCalled();
    expect(outcome.rethrown).toBeTrue();
  });

  for (const status of [400, 404, 409]) {
    it(`passes ${status} through to the page untouched`, () => {
      const outcome = fail(status, true);

      expect(notifications.error).not.toHaveBeenCalled();
      expect(auth.logout).not.toHaveBeenCalled();
      expect(outcome.rethrown).toBeTrue();
    });
  }
});

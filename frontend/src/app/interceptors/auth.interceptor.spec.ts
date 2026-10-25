import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { API_CONFIG } from '../api-config';
import { AuthStore } from '../auth/auth.store';
import { testApiConfig } from '../testing/test-data';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const token = signal<string | null>('the-token');
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    token.set('the-token');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: testApiConfig },
        { provide: AuthStore, useValue: { token } },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function authorizationFor(url: string): string | null {
    http.get(url).subscribe();
    const request = backend.expectOne(url);
    request.flush({});
    return request.request.headers.get('Authorization');
  }

  it('adds the bearer token to Identity.Api requests', () => {
    expect(authorizationFor('http://identity.test/api/auth/me')).toBe('Bearer the-token');
  });

  it('adds the bearer token to Expenses.Api requests', () => {
    expect(authorizationFor('http://expenses.test/api/expenses?page=1')).toBe('Bearer the-token');
  });

  it('never sends the token to another host', () => {
    expect(authorizationFor('https://evil.example/api/expenses')).toBeNull();
  });

  it('does not match a host that only starts like ours', () => {
    expect(authorizationFor('http://expenses.test/api-evil/steal')).toBeNull();
    expect(authorizationFor('http://expenses.test.evil.example/api/expenses')).toBeNull();
  });

  it('sends no header when signed out', () => {
    token.set(null);

    expect(authorizationFor('http://expenses.test/api/expenses')).toBeNull();
  });
});

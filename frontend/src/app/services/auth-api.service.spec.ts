import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_CONFIG } from '../api-config';
import { AuthResponse, User } from '../models/auth.models';
import { testApiConfig, testSession } from '../testing/test-data';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let api: AuthApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: testApiConfig },
      ],
    });
    api = TestBed.inject(AuthApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login posts the credentials to Identity.Api', () => {
    let response: AuthResponse | undefined;

    api
      .login({ email: 'demo@ballastlane.com', password: 'Demo123!' })
      .subscribe((r) => (response = r));

    const request = http.expectOne('http://identity.test/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'demo@ballastlane.com', password: 'Demo123!' });
    request.flush(testSession());
    expect(response?.user.email).toBe('demo@ballastlane.com');
  });

  it('register posts the new account to Identity.Api', () => {
    const body = { email: 'new@example.com', displayName: 'New User', password: 'Demo123!' };

    api.register(body).subscribe();

    const request = http.expectOne('http://identity.test/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(testSession());
  });

  it('me gets the current user', () => {
    let user: User | undefined;

    api.me().subscribe((u) => (user = u));

    const request = http.expectOne('http://identity.test/api/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush(testSession().user);
    expect(user?.displayName).toBe('Demo User');
  });
});

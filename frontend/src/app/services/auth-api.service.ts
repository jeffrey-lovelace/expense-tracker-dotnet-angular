import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../api-config';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';

/** Identity.Api over HTTP. The only auth code that knows URLs. */
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_CONFIG).identityApiUrl}/auth`;

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request);
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request);
  }

  me(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`);
  }
}

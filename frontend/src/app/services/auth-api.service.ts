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
    throw new Error(`Not implemented: ${this.baseUrl} ${request.email}`);
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    throw new Error(`Not implemented: ${this.baseUrl} ${request.email}`);
  }

  me(): Observable<User> {
    throw new Error(`Not implemented: ${this.http}`);
  }
}

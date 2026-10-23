import { computed, inject, Injectable, Signal, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { LoginRequest, RegisterRequest, Session, User } from '../models/auth.models';
import { AuthApiService } from '../services/auth-api.service';
import { TokenStorageService } from './token-storage.service';

/** Current user and token. Private writable signal, public read-only signals (AGENTS.md 10.2). */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApiService);
  private readonly storage = inject(TokenStorageService);
  private readonly session = signal<Session | null>(null);

  readonly user: Signal<User | null> = computed(() => null);
  readonly token: Signal<string | null> = computed(() => null);
  readonly isAuthenticated: Signal<boolean> = computed(() => false);

  login(request: LoginRequest): Observable<User> {
    throw new Error(
      `Not implemented: ${request.email} ${this.api} ${this.storage} ${this.session()}`,
    );
  }

  register(request: RegisterRequest): Observable<User> {
    throw new Error(`Not implemented: ${request.email}`);
  }

  logout(): void {
    throw new Error('Not implemented');
  }
}

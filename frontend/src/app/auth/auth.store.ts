import { computed, inject, Injectable, Signal, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';

import { AuthResponse, LoginRequest, RegisterRequest, Session, User } from '../models/auth.models';
import { AuthApiService } from '../services/auth-api.service';
import { TokenStorageService } from './token-storage.service';

/**
 * Current user and token. Private writable signal, public read-only signals (AGENTS.md 10.2).
 * RxJS stays at the HTTP boundary: login and register return the request so the page can react to success or
 * failure; the store only records a successful session.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApiService);
  private readonly storage = inject(TokenStorageService);

  // Restored from sessionStorage, so a refresh keeps the user signed in.
  private readonly session = signal<Session | null>(this.storage.read());

  readonly user: Signal<User | null> = computed(() => this.session()?.user ?? null);
  readonly token: Signal<string | null> = computed(() => this.session()?.token ?? null);

  /**
   * True while there is a session that has not expired. A plain function, not a `computed`: a computed caches its
   * value until a signal changes, and the clock is not a signal, so it would keep saying "signed in" after expiry.
   * It still reads the session signal, so templates update on login and logout.
   */
  readonly isAuthenticated = (): boolean => {
    const session = this.session();
    return session !== null && Date.parse(session.expiresAtUtc) > Date.now();
  };

  login(request: LoginRequest): Observable<User> {
    return this.api.login(request).pipe(
      tap((response) => this.start(response)),
      map((response) => response.user),
    );
  }

  /** Registering signs the new user in right away. */
  register(request: RegisterRequest): Observable<User> {
    return this.api.register(request).pipe(
      tap((response) => this.start(response)),
      map((response) => response.user),
    );
  }

  logout(): void {
    this.storage.clear();
    this.session.set(null);
  }

  private start(response: AuthResponse): void {
    this.storage.save(response);
    this.session.set(response);
  }
}

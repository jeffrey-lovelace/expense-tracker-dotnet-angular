// Shapes of the Identity.Api contract (AGENTS.md 10.9). Field names match the camelCase JSON.

export interface User {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly email: string;
  readonly displayName: string;
  readonly password: string;
}

/** Returned by register and login. */
export interface AuthResponse {
  readonly token: string;
  /** ISO 8601 UTC instant, for example "2026-10-23T03:00:00Z". */
  readonly expiresAtUtc: string;
  readonly user: User;
}

/** What the app keeps between page loads: exactly what login returned. */
export type Session = AuthResponse;

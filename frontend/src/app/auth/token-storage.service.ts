import { Injectable } from '@angular/core';

import { Session } from '../models/auth.models';

/**
 * Keeps a copy of the session in sessionStorage so a page refresh does not sign the user out. Closing the tab
 * clears it. Trade-off (AGENTS.md section 8): an httpOnly cookie resists XSS better but needs CSRF protection
 * across two APIs; with short-lived tokens and Angular's template sanitization this is acceptable here.
 *
 * Storage can be unavailable (private mode, blocked site data), so every access is guarded: the app then simply
 * keeps the session in memory only.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  static readonly key = 'et.session';

  read(): Session | null {
    let raw: string | null;
    try {
      raw = sessionStorage.getItem(TokenStorageService.key);
    } catch {
      return null;
    }

    if (raw === null) {
      return null;
    }

    const session = parse(raw);
    if (session === null || Date.parse(session.expiresAtUtc) <= Date.now()) {
      this.clear();
      return null;
    }

    return session;
  }

  save(session: Session): void {
    try {
      sessionStorage.setItem(TokenStorageService.key, JSON.stringify(session));
    } catch {
      // Storage unavailable: the session still lives in memory for this page.
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(TokenStorageService.key);
    } catch {
      // Nothing to clear.
    }
  }
}

// Anything stored can be edited by hand, so the shape is checked before it is trusted.
function parse(raw: string): Session | null {
  try {
    const value: unknown = JSON.parse(raw);
    if (!isRecord(value) || !isRecord(value['user'])) {
      return null;
    }

    const { token, expiresAtUtc, user } = value as Record<string, unknown> & {
      user: Record<string, unknown>;
    };
    const valid =
      typeof token === 'string' &&
      typeof expiresAtUtc === 'string' &&
      !Number.isNaN(Date.parse(expiresAtUtc)) &&
      typeof user['id'] === 'string' &&
      typeof user['email'] === 'string' &&
      typeof user['displayName'] === 'string';

    return valid ? (value as unknown as Session) : null;
  } catch {
    return null;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

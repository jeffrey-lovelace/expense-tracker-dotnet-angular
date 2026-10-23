import { Injectable } from '@angular/core';

import { Session } from '../models/auth.models';

/**
 * Keeps a copy of the session in sessionStorage so a page refresh does not sign the user out. Closing the tab
 * clears it. Trade-off (AGENTS.md section 8): an httpOnly cookie resists XSS better but needs CSRF protection
 * across two APIs; with short-lived tokens and Angular's template sanitization this is acceptable here.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  static readonly key = 'et.session';

  read(): Session | null {
    throw new Error('Not implemented');
  }

  save(session: Session): void {
    throw new Error(`Not implemented: ${session.user.email}`);
  }

  clear(): void {
    throw new Error('Not implemented');
  }
}

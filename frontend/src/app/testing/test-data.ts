import { ApiConfig } from '../api-config';
import { Session } from '../models/auth.models';

export const testApiConfig: ApiConfig = {
  identityApiUrl: 'http://identity.test/api',
  expensesApiUrl: 'http://expenses.test/api',
};

/** A session that expires one hour after `now` (defaults to the real clock). */
export function testSession(now: Date = new Date()): Session {
  return {
    token: 'header.payload.signature',
    expiresAtUtc: new Date(now.getTime() + 60 * 60 * 1000).toISOString(),
    user: {
      id: '5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f',
      email: 'demo@ballastlane.com',
      displayName: 'Demo User',
    },
  };
}

import { TestBed } from '@angular/core/testing';

import { testSession } from '../testing/test-data';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let storage: TokenStorageService;

  beforeEach(() => {
    sessionStorage.clear();
    storage = TestBed.inject(TokenStorageService);
  });

  afterEach(() => sessionStorage.clear());

  it('reads back what it saved', () => {
    const session = testSession();

    storage.save(session);

    expect(storage.read()).toEqual(session);
  });

  it('returns null when nothing is stored', () => {
    expect(storage.read()).toBeNull();
  });

  it('returns null and forgets a corrupt value', () => {
    sessionStorage.setItem(TokenStorageService.key, '{not json');

    expect(storage.read()).toBeNull();
    expect(sessionStorage.getItem(TokenStorageService.key)).toBeNull();
  });

  it('returns null for a value with the wrong shape', () => {
    sessionStorage.setItem(TokenStorageService.key, JSON.stringify({ token: 42 }));

    expect(storage.read()).toBeNull();
  });

  it('returns null and forgets an expired session', () => {
    storage.save(testSession(new Date(Date.now() - 2 * 60 * 60 * 1000)));

    expect(storage.read()).toBeNull();
    expect(sessionStorage.getItem(TokenStorageService.key)).toBeNull();
  });

  it('clear removes the session', () => {
    storage.save(testSession());

    storage.clear();

    expect(storage.read()).toBeNull();
  });

  it('keeps working when sessionStorage is unavailable (private mode, blocked storage)', () => {
    spyOn(sessionStorage, 'getItem').and.throwError('SecurityError');
    spyOn(sessionStorage, 'setItem').and.throwError('SecurityError');
    spyOn(sessionStorage, 'removeItem').and.throwError('SecurityError');

    expect(() => storage.save(testSession())).not.toThrow();
    expect(storage.read()).toBeNull();
    expect(() => storage.clear()).not.toThrow();
  });
});

import { defaultAfterLogin, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  for (const url of ['/expenses', '/expenses?page=2&category=Food', '/expenses#top']) {
    it(`follows the in-app path ${url}`, () => {
      expect(safeReturnUrl(url)).toBe(url);
    });
  }

  for (const url of [
    undefined,
    null,
    '',
    'https://evil.example/phish',
    '//evil.example/phish',
    '/\\evil.example',
    'javascript:alert(1)',
    'expenses',
    '/login',
    '/register',
  ]) {
    it(`falls back to ${defaultAfterLogin} for ${String(url)}`, () => {
      expect(safeReturnUrl(url)).toBe(defaultAfterLogin);
    });
  }
});

/** Where to go after signing in when there is no usable returnUrl. */
export const defaultAfterLogin = '/expenses';

/**
 * Follows a returnUrl only when it is a path inside this app. Anything else (another origin, a protocol-relative
 * "//host" URL, "javascript:") falls back to the default, so /login?returnUrl=... cannot redirect off the site.
 * Going "back" to /login or /register would be a loop, so those fall back too.
 */
export function safeReturnUrl(returnUrl: string | null | undefined): string {
  if (
    !returnUrl ||
    !returnUrl.startsWith('/') ||
    returnUrl.startsWith('//') ||
    returnUrl.startsWith('/\\')
  ) {
    return defaultAfterLogin;
  }

  const path = returnUrl.split(/[?#]/, 1)[0];
  return path === '/login' || path === '/register' ? defaultAfterLogin : returnUrl;
}

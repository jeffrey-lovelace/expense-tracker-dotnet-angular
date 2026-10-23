/** Where to go after signing in when there is no usable returnUrl. */
export const defaultAfterLogin = '/expenses';

/**
 * Follows a returnUrl only when it is a path inside this app. Anything else (another origin, a protocol-relative
 * "//host" URL, "javascript:") falls back to the default, so /login?returnUrl=... cannot redirect off the site.
 */
export function safeReturnUrl(returnUrl: string | null | undefined): string {
  throw new Error(`Not implemented: ${returnUrl}`);
}

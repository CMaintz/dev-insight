import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthTokenStore } from '../auth/auth-token.store';
import { AppConfig } from '../config/app-config';

/** True for root-relative API paths: `/api/...` and `/health`. */
export function isApiPath(url: string): boolean {
  return url.startsWith('/api/') || url === '/health' || url.startsWith('/health?');
}

/**
 * Adds `Authorization: Bearer <token>` to API requests when a token exists. Runs before the
 * base-URL interceptor so it only ever matches our own relative API paths — the token never
 * leaves for third-party URLs.
 */
export const bearerTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthTokenStore).token();
  if (!token || !isApiPath(req.url) || req.headers.has('Authorization')) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

/** Prefixes API paths with the runtime `apiBaseUrl` (empty = same origin, unchanged). */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const config = inject(AppConfig);
  if (!config.apiBaseUrl() || !isApiPath(req.url)) {
    return next(req);
  }
  return next(req.clone({ url: config.apiUrl(req.url) }));
};

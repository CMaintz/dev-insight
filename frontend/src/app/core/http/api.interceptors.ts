import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthTokenStore } from '../auth/auth-token.store';
import { AppConfig } from '../config/app-config';

function isApiPath(url: string): boolean {
  return url.startsWith('/api/') || url === '/health' || url.startsWith('/health?');
}

export const bearerTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthTokenStore).token();
  if (!token || !isApiPath(req.url) || req.headers.has('Authorization')) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const config = inject(AppConfig);
  if (!config.apiBaseUrl() || !isApiPath(req.url)) {
    return next(req);
  }
  return next(req.clone({ url: config.apiUrl(req.url) }));
};

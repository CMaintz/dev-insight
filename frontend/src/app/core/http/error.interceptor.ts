import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthTokenStore } from '../auth/auth-token.store';
import { SessionStore } from '../auth/session.store';
import { ToastService } from '../notifications/toast.service';
import { describeHttpError } from './problem-details';

/** Set to true on a request whose errors the caller renders itself (no toast). */
export const SILENT_ERRORS = new HttpContextToken<boolean>(() => false);

/** Requests whose 401 is an expected answer, not an expired session. */
const PUBLIC_PATHS = ['/api/me'];
const PUBLIC_PREFIXES = ['/api/portfolio/'];

export function isPublicRequest(url: string): boolean {
  const path = url.replace(/^https?:\/\/[^/]+/, '').split('?')[0];
  return PUBLIC_PATHS.includes(path) || PUBLIC_PREFIXES.some((prefix) => path.startsWith(prefix));
}

/**
 * 401 → drop the bearer token and session and return to the landing page (except the session probe and the
 * public portfolio). 404 is left to pages ("not analysed yet", "portfolio not found").
 * Everything else surfaces as a toast built from the problem-details body.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const toasts = inject(ToastService);
  const tokens = inject(AuthTokenStore);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }
      if (error.status === 401) {
        if (!isPublicRequest(req.url)) {
          tokens.clear();
          session.markSignedOut();
          void router.navigateByUrl('/');
        }
      } else if (error.status !== 404 && !req.context.get(SILENT_ERRORS)) {
        const message = describeHttpError(error);
        toasts.error(message.title, message.detail);
      }
      return throwError(() => error);
    }),
  );
};

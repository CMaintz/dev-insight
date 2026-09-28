import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthTokenStore } from '../auth/auth-token.store';
import { SessionStore } from '../auth/session.store';
import { ToastService } from '../notifications/toast.service';
import { describeHttpError } from './problem-details';

export const SILENT_ERRORS = new HttpContextToken<boolean>(() => false);

const PUBLIC_PATHS = ['/api/me'];
const PUBLIC_PREFIXES = ['/api/portfolio/'];

function isPublicRequest(url: string): boolean {
  const path = url.replace(/^https?:\/\/[^/]+/, '').split('?')[0];
  return PUBLIC_PATHS.includes(path) || PUBLIC_PREFIXES.some((prefix) => path.startsWith(prefix));
}

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

import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { ToastService } from '../notifications/toast.service';

const STATUSES_THE_INTERCEPTOR_LEAVES_TO_CALLERS = new Set([401, 404]);

function isToastedByInterceptor(error: unknown): error is HttpErrorResponse {
  return (
    error instanceof HttpErrorResponse &&
    !STATUSES_THE_INTERCEPTOR_LEAVES_TO_CALLERS.has(error.status)
  );
}

@Injectable({ providedIn: 'root' })
export class UserActionErrors {
  private readonly toasts = inject(ToastService);

  handle(error: unknown, notFoundMessage: string): undefined {
    if (isToastedByInterceptor(error)) {
      return undefined;
    }
    if (error instanceof HttpErrorResponse && error.status === 404) {
      this.toasts.error('Not found', notFoundMessage);
      return undefined;
    }
    throw error;
  }

  async resultOrNothing<T>(
    request: Observable<T>,
    notFoundMessage: string,
  ): Promise<T | undefined> {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      return this.handle(error, notFoundMessage);
    }
  }
}

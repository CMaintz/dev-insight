import { Injectable, signal } from '@angular/core';

export const TOKEN_STORAGE_KEY = 'devinsight.token';

function readStored(): string | null {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStored(token: string | null): void {
  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
  } catch {
    // Storage unavailable (private mode, blocked site data): the in-memory copy still works
    // for this tab.
  }
}

/**
 * Bearer token for the cross-origin API. Persisted in localStorage when available, with an
 * in-memory fallback. The token is never logged or sent anywhere but the API.
 */
@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly current = signal<string | null>(readStored());

  readonly token = this.current.asReadonly();

  set(token: string): void {
    this.current.set(token);
    writeStored(token);
  }

  clear(): void {
    this.current.set(null);
    writeStored(null);
  }
}

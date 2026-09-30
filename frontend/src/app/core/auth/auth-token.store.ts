import { Injectable, signal } from '@angular/core';
import { readStoredValue, storeValueIfPossible } from '../../shared/util/browser-storage';

const TOKEN_STORAGE_KEY = 'devinsight.token';

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly current = signal<string | null>(readStoredValue(TOKEN_STORAGE_KEY));

  readonly token = this.current.asReadonly();

  set(token: string): void {
    this.current.set(token);
    storeValueIfPossible(TOKEN_STORAGE_KEY, token);
  }

  clear(): void {
    this.current.set(null);
    storeValueIfPossible(TOKEN_STORAGE_KEY, null);
  }
}

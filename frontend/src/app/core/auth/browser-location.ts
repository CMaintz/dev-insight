import { InjectionToken } from '@angular/core';

/** Abstraction over full-page navigation so tests can observe OAuth redirects. */
export interface BrowserLocation {
  assign(url: string): void;
}

export const BROWSER_LOCATION = new InjectionToken<BrowserLocation>('BROWSER_LOCATION', {
  providedIn: 'root',
  factory: () => ({ assign: (url: string) => window.location.assign(url) }),
});

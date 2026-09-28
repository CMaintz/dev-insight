import { InjectionToken } from '@angular/core';

export interface BrowserLocation {
  assign(url: string): void;
  reload(): void;
}

export const BROWSER_LOCATION = new InjectionToken<BrowserLocation>('BROWSER_LOCATION', {
  providedIn: 'root',
  factory: () => ({
    assign: (url: string) => window.location.assign(url),
    reload: () => window.location.reload(),
  }),
});

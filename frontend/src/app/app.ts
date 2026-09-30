import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BROWSER_LOCATION } from './core/auth/browser-location';
import { SessionStore } from './core/auth/session.store';
import { AppConfig } from './core/config/app-config';
import { AppHeader } from './core/layout/app-header';
import { ToastHost } from './core/notifications/toast-host';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AppHeader, ToastHost],
  template: `
    @if (config.loadFailed()) {
      <main id="main" class="config-error" role="alert">
        <h1>Couldn't load app configuration</h1>
        <p>DevInsight could not read its settings, so it cannot reach the API. Please try again.</p>
        <button type="button" class="btn btn--primary" (click)="reload()">Reload</button>
      </main>
    } @else {
      <app-header />
      <main id="main" tabindex="-1">
        <router-outlet />
      </main>
      <app-toast-host />
    }
  `,
  styles: `
    main {
      display: block;
      outline: none;
    }
    .config-error {
      display: grid;
      gap: 1rem;
      max-width: 32rem;
      margin: 4rem auto;
      padding: 0 1rem;
    }
  `,
})
export class App {
  protected readonly config = inject(AppConfig);
  private readonly session = inject(SessionStore);
  private readonly location = inject(BROWSER_LOCATION);

  constructor() {
    if (!this.config.loadFailed()) {
      void this.session.ensureLoaded();
    }
  }

  protected reload(): void {
    this.location.reload();
  }
}

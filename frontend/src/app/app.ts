import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionStore } from './core/auth/session.store';
import { AppHeader } from './core/layout/app-header';
import { ToastHost } from './core/notifications/toast-host';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AppHeader, ToastHost],
  template: `
    <app-header />
    <main id="main" tabindex="-1">
      <router-outlet />
    </main>
    <app-toast-host />
  `,
  styles: `
    main {
      display: block;
      outline: none;
    }
  `,
})
export class App {
  constructor() {
    // Instantiate the theme early so `data-theme` is applied before the first route renders,
    // and resolve the session in the background so the header knows who is signed in.
    inject(ThemeService);
    void inject(SessionStore).ensureLoaded();
  }
}

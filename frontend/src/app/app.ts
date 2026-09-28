import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionStore } from './core/auth/session.store';
import { AppHeader } from './core/layout/app-header';
import { ToastHost } from './core/notifications/toast-host';

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
  private readonly session = inject(SessionStore);

  constructor() {
    void this.session.ensureLoaded();
  }
}

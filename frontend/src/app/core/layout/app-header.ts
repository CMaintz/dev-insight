import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { jumpToFragment } from '../../shared/util/jump-to';
import { SessionStore } from '../auth/session.store';
import { ThemeService } from '../theme/theme.service';

@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss',
})
export class AppHeader {
  private readonly router = inject(Router);
  protected readonly session = inject(SessionStore);
  protected readonly theme = inject(ThemeService);

  protected readonly links = [
    { path: '/dashboard', label: 'Dashboard' },
    { path: '/repositories', label: 'Repositories' },
    { path: '/projects', label: 'Projects' },
    { path: '/settings', label: 'Settings' },
  ];

  protected readonly jumpTo = jumpToFragment;

  protected signIn(): void {
    const current = this.router.url;
    this.session.signIn(current === '/' ? '/dashboard' : current);
  }

  protected signOut(): void {
    void this.session.logout();
  }
}

import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth.api';
import { AuthTokenStore } from '../../core/auth/auth-token.store';
import { SessionStore, safeReturnUrl } from '../../core/auth/session.store';

type Phase = 'working' | 'failed';

const MESSAGES = {
  denied: 'GitHub sign-in did not complete. You may have cancelled it, or GitHub refused access.',
  missing: 'This sign-in link is incomplete. Please start again.',
  exchange: 'We could not finish signing you in — the link may have expired. Please try again.',
} as const;

@Component({
  selector: 'app-auth-callback-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="page callback">
      @if (phase() === 'working') {
        <section class="card" role="status" aria-live="polite">
          <h1>Signing you in…</h1>
          <div class="progress" aria-hidden="true"></div>
        </section>
      } @else {
        <section class="card" role="alert">
          <h1>Sign-in failed</h1>
          <p class="muted">{{ message() }}</p>
          <div class="page-actions">
            <button type="button" class="btn btn--primary" (click)="retry()">Try again</button>
            <a class="btn" routerLink="/">Back to the start</a>
          </div>
        </section>
      }
    </div>
  `,
  styles: `
    .callback {
      max-width: 32rem;
    }
    .card {
      display: grid;
      gap: 1rem;
      margin-top: 3rem;
    }
    h1 {
      margin: 0;
      font-size: 1.5rem;
    }
    p {
      margin: 0;
    }
  `,
})
export class AuthCallbackPage implements OnInit {
  private readonly api = inject(AuthApi);
  private readonly tokens = inject(AuthTokenStore);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly code = input<string | undefined>(undefined);
  readonly returnUrl = input<string | undefined>(undefined);
  readonly error = input<string | undefined>(undefined);

  protected readonly phase = signal<Phase>('working');
  protected readonly message = signal<string>(MESSAGES.exchange);

  ngOnInit(): void {
    void this.complete();
  }

  protected retry(): void {
    this.session.signIn(safeReturnUrl(this.returnUrl()));
  }

  private async complete(): Promise<void> {
    const code = this.code();
    if (this.error()) {
      return this.fail(MESSAGES.denied);
    }
    if (!code) {
      return this.fail(MESSAGES.missing);
    }
    try {
      const response = await firstValueFrom(this.api.exchange(code));
      this.tokens.set(response.accessToken);
    } catch {
      return this.fail(MESSAGES.exchange);
    }
    await this.session.refresh();
    await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()), { replaceUrl: true });
  }

  private fail(message: string): void {
    this.message.set(message);
    this.phase.set('failed');
  }
}

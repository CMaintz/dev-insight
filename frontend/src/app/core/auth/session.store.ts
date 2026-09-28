import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../api/profile.api';
import { AppConfig } from '../config/app-config';
import { Profile } from '../models/api.models';
import { AuthTokenStore } from './auth-token.store';
import { BROWSER_LOCATION } from './browser-location';

export type SessionStatus = 'unknown' | 'authenticated' | 'anonymous';

export const DEFAULT_RETURN_URL = '/dashboard';

/**
 * Only app-local paths are allowed as post-login destinations (no open redirects):
 * must start with a single `/`, and must not smuggle a host via `//` or `/\`.
 */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return DEFAULT_RETURN_URL;
  }
  return value;
}

/** Builds the backend OAuth entry URL (the backend also only accepts local return paths). */
export function gitHubLoginUrl(returnUrl: string, apiBaseUrl = ''): string {
  const base = apiBaseUrl.replace(/\/+$/, '');
  return `${base}/api/auth/github/login?returnUrl=${encodeURIComponent(safeReturnUrl(returnUrl))}`;
}

interface SessionState {
  status: SessionStatus;
  profile: Profile | null;
}

/** Single source of truth for "who is signed in", backed by `GET /api/me`. */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly api = inject(ProfileApi);
  private readonly router = inject(Router);
  private readonly location = inject(BROWSER_LOCATION);
  private readonly config = inject(AppConfig);
  private readonly tokens = inject(AuthTokenStore);

  private readonly state = signal<SessionState>({ status: 'unknown', profile: null });
  private pending: Promise<Profile | null> | null = null;

  readonly status = computed(() => this.state().status);
  readonly profile = computed(() => this.state().profile);
  readonly isAuthenticated = computed(() => this.state().status === 'authenticated');

  /** Resolves the session once; concurrent callers share the same request. */
  ensureLoaded(): Promise<Profile | null> {
    if (this.state().status !== 'unknown') {
      return Promise.resolve(this.state().profile);
    }
    this.pending ??= firstValueFrom(this.api.me()).then(
      (profile) => {
        this.setProfile(profile);
        return profile;
      },
      (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          // A stored token the API rejects is stale — drop it.
          this.tokens.clear();
        }
        this.markSignedOut();
        return null;
      },
    );
    return this.pending;
  }

  /** Forgets the cached profile and probes `/api/me` again (e.g. after a token exchange). */
  refresh(): Promise<Profile | null> {
    this.pending = null;
    this.state.set({ status: 'unknown', profile: null });
    return this.ensureLoaded();
  }

  setProfile(profile: Profile): void {
    this.pending = null;
    this.state.set({ status: 'authenticated', profile });
  }

  markSignedOut(): void {
    this.pending = null;
    this.state.set({ status: 'anonymous', profile: null });
  }

  signIn(returnUrl = DEFAULT_RETURN_URL): void {
    this.location.assign(gitHubLoginUrl(returnUrl, this.config.apiBaseUrl()));
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.api.logout());
    } catch {
      // Logout is stateless server-side; clearing the token below is what matters.
    } finally {
      this.tokens.clear();
      this.markSignedOut();
      await this.router.navigateByUrl('/');
    }
  }
}

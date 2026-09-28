import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../api/profile.api';
import { resultOrNothing } from '../http/surfaced-errors';
import { AppConfig } from '../config/app-config';
import { Profile } from '../models/api.models';
import { AuthTokenStore } from './auth-token.store';
import { BROWSER_LOCATION } from './browser-location';

type SessionStatus = 'unknown' | 'authenticated' | 'anonymous';

const DEFAULT_RETURN_URL = '/dashboard';

export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return DEFAULT_RETURN_URL;
  }
  return value;
}

function gitHubLoginUrl(returnUrl: string, apiBaseUrl = ''): string {
  const base = apiBaseUrl.replace(/\/+$/, '');
  return `${base}/api/auth/github/login?returnUrl=${encodeURIComponent(safeReturnUrl(returnUrl))}`;
}

interface SessionState {
  status: SessionStatus;
  profile: Profile | null;
}

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
        this.dropTokenIfRejected(error);
        this.markSignedOut();
        return null;
      },
    );
    return this.pending;
  }

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

  private dropTokenIfRejected(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 401) {
      this.tokens.clear();
    }
  }

  signIn(returnUrl = DEFAULT_RETURN_URL): void {
    this.location.assign(gitHubLoginUrl(returnUrl, this.config.apiBaseUrl()));
  }

  async logout(): Promise<void> {
    await resultOrNothing(this.api.logout());
    this.tokens.clear();
    this.markSignedOut();
    await this.router.navigateByUrl('/');
  }
}

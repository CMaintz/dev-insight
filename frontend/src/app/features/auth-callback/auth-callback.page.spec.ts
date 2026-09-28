import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthTokenStore } from '../../core/auth/auth-token.store';
import { BROWSER_LOCATION } from '../../core/auth/browser-location';
import { SessionStore } from '../../core/auth/session.store';
import { AppConfig } from '../../core/config/app-config';
import { aProfile } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { AuthCallbackPage } from './auth-callback.page';

function start(inputs: Record<string, unknown>) {
  localStorage.clear();
  const assign = vi.fn();
  TestBed.overrideProvider(BROWSER_LOCATION, { useValue: { assign } });
  const page = createPage(AuthCallbackPage, inputs, null);
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  return { ...page, assign, navigate };
}

describe('AuthCallbackPage', () => {
  it('exchanges the code, stores the token, loads the session and continues', async () => {
    const { http, element, settle, navigate } = start({ code: 'one-time', returnUrl: '/projects' });
    await settle();
    expect(text(element)).toContain('Signing you in…');

    const exchange = http.expectOne({ method: 'POST', url: '/api/auth/exchange' });
    expect(exchange.request.body).toEqual({ code: 'one-time' });
    exchange.flush({ accessToken: 'jwt-1', expiresAt: '2026-10-01T00:00:00+00:00' });
    await settle();

    expect(TestBed.inject(AuthTokenStore).token()).toBe('jwt-1');
    http.expectOne('/api/me').flush(aProfile());
    await settle();
    expect(TestBed.inject(SessionStore).isAuthenticated()).toBe(true);
    expect(navigate).toHaveBeenCalledWith('/projects', { replaceUrl: true });
  });

  it('refuses non-local return URLs', async () => {
    const { http, settle, navigate } = start({ code: 'c', returnUrl: 'https://evil.example/x' });
    await settle();
    http.expectOne('/api/auth/exchange').flush({ accessToken: 't', expiresAt: 'x' });
    await settle();
    http.expectOne('/api/me').flush(aProfile());
    await settle();
    expect(navigate).toHaveBeenCalledWith('/dashboard', { replaceUrl: true });
  });

  it('shows a friendly error with retry when the exchange fails', async () => {
    const { http, element, settle, assign } = start({ code: 'expired', returnUrl: '/settings' });
    TestBed.inject(AppConfig).set({ apiBaseUrl: 'https://api.example' });
    await settle();
    http
      .expectOne('/api/auth/exchange')
      .flush({ title: 'Invalid code' }, { status: 400, statusText: 'Bad Request' });
    await settle();

    expect(text(element)).toContain('Sign-in failed');
    expect(text(element)).toContain('may have expired');
    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
    button(element, 'Try again').click();
    expect(assign).toHaveBeenCalledWith(
      'https://api.example/api/auth/github/login?returnUrl=%2Fsettings',
    );
  });

  it('reports a denied sign-in without calling the API', async () => {
    const { http, element, settle } = start({ error: 'signin_failed' });
    await settle();
    http.expectNone('/api/auth/exchange');
    expect(text(element)).toContain('GitHub sign-in did not complete');
  });

  it('reports a missing code', async () => {
    const { element, settle } = start({});
    await settle();
    expect(text(element)).toContain('incomplete');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { aProfile } from '../../../testing/fixtures';
import { RunTracker } from '../analysis/run-tracker';
import { AppConfig } from '../config/app-config';
import { AuthTokenStore } from './auth-token.store';
import { BROWSER_LOCATION } from './browser-location';
import { SessionStore, safeReturnUrl } from './session.store';

describe('SessionStore', () => {
  let store: SessionStore;
  let controller: HttpTestingController;
  let assign: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    localStorage.clear();
    assign = vi.fn();
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BROWSER_LOCATION, useValue: { assign } },
        { provide: Router, useValue: { navigateByUrl: navigate } },
      ],
    });
    store = TestBed.inject(SessionStore);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('loads the profile once and shares the in-flight request', async () => {
    const first = store.ensureLoaded();
    const second = store.ensureLoaded();
    controller.expectOne('/api/me').flush(aProfile());

    expect(await first).toEqual(aProfile());
    expect(await second).toEqual(aProfile());
    expect(store.isAuthenticated()).toBe(true);
    expect(await store.ensureLoaded()).toEqual(aProfile());
  });

  it('treats a failed probe as anonymous and drops a rejected token', async () => {
    TestBed.inject(AuthTokenStore).set('stale');
    const pending = store.ensureLoaded();
    controller.expectOne('/api/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(await pending).toBeNull();
    expect(store.status()).toBe('anonymous');
    expect(store.profile()).toBeNull();
    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
  });

  it('keeps the token when the probe fails for other reasons', async () => {
    TestBed.inject(AuthTokenStore).set('ok');
    const pending = store.ensureLoaded();
    controller.expectOne('/api/me').flush(null, { status: 503, statusText: 'Down' });
    await pending;
    expect(TestBed.inject(AuthTokenStore).token()).toBe('ok');
  });

  it('ignores a stale probe 401 that answers after a fresh token was exchanged', async () => {
    const bootProbe = store.ensureLoaded();
    const [staleRequest] = controller.match('/api/me');

    TestBed.inject(AuthTokenStore).set('fresh');
    const refreshed = store.refresh();
    const [freshRequest] = controller.match('/api/me');

    staleRequest.flush(null, { status: 401, statusText: 'Unauthorized' });
    await bootProbe;
    expect(TestBed.inject(AuthTokenStore).token()).toBe('fresh');
    expect(store.status()).toBe('unknown');

    freshRequest.flush(aProfile());
    await refreshed;
    expect(store.isAuthenticated()).toBe(true);
    expect(TestBed.inject(AuthTokenStore).token()).toBe('fresh');
  });

  it('stops all analysis polling when the session ends', () => {
    const stopAll = vi.spyOn(TestBed.inject(RunTracker), 'stopAll');
    store.markSignedOut();
    expect(stopAll).toHaveBeenCalled();
  });

  it('refresh re-probes the profile', async () => {
    store.setProfile(aProfile());
    const pending = store.refresh();
    expect(store.status()).toBe('unknown');
    controller.expectOne('/api/me').flush(aProfile({ login: 'new' }));
    expect((await pending)?.login).toBe('new');
  });

  it('never hands a non-local return URL to the OAuth start', () => {
    store.signIn('https://evil.example');
    store.signIn('//evil.example');
    expect(assign).toHaveBeenNthCalledWith(1, '/api/auth/github/login?returnUrl=%2Fdashboard');
    expect(assign).toHaveBeenNthCalledWith(2, '/api/auth/github/login?returnUrl=%2Fdashboard');
  });

  it('signs in against the configured API origin', () => {
    TestBed.inject(AppConfig).set({ apiBaseUrl: 'https://api.example/' });
    store.signIn('/settings');
    expect(assign).toHaveBeenCalledWith(
      'https://api.example/api/auth/github/login?returnUrl=%2Fsettings',
    );
  });

  it('starts the GitHub OAuth flow with a local return path', () => {
    store.signIn('/repositories');
    expect(assign).toHaveBeenCalledWith('/api/auth/github/login?returnUrl=%2Frepositories');
  });

  it('logs out, clears the session and token and returns home', async () => {
    TestBed.inject(AuthTokenStore).set('tok');
    store.setProfile(aProfile());
    const done = store.logout();
    controller.expectOne({ method: 'POST', url: '/api/auth/logout' }).flush(null);
    await done;

    expect(store.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith('/');
    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
  });

  it('still clears the token when the logout call fails', async () => {
    TestBed.inject(AuthTokenStore).set('tok');
    const done = store.logout();
    controller.expectOne('/api/auth/logout').flush(null, { status: 500, statusText: 'x' });
    await done;
    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
  });
});

describe('safeReturnUrl', () => {
  it('accepts local paths only', () => {
    expect(safeReturnUrl('/repositories/r1?tab=x')).toBe('/repositories/r1?tab=x');
    expect(safeReturnUrl(undefined)).toBe('/dashboard');
    expect(safeReturnUrl('dashboard')).toBe('/dashboard');
    expect(safeReturnUrl('//evil.example')).toBe('/dashboard');
    expect(safeReturnUrl(String.raw`/\evil.example`)).toBe('/dashboard');
    expect(safeReturnUrl('https://evil.example')).toBe('/dashboard');
  });
});

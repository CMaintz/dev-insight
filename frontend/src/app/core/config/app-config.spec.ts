import { TestBed } from '@angular/core/testing';
import { AppConfig, loadAppConfig } from './app-config';

function response(body: unknown, ok = true): Response {
  return { ok, json: () => Promise.resolve(body) } as Response;
}

describe('loadAppConfig', () => {
  it('fetches config.json relative to the base href, bypassing the cache', async () => {
    const fetchFn = vi.fn().mockResolvedValue(response({ apiBaseUrl: 'https://api.example/' }));
    const config = await loadAppConfig(fetchFn, 'https://cmaintz.github.io/DevInsight/');
    expect(fetchFn).toHaveBeenCalledWith('https://cmaintz.github.io/DevInsight/config.json', {
      cache: 'no-store',
    });
    expect(config).toEqual({ apiBaseUrl: 'https://api.example' });
  });

  it('reports failure (null) for HTTP errors, bad JSON, network failure or a malformed value', async () => {
    const base = 'http://localhost:4200/';
    const bodies: [unknown, boolean][] = [
      [null, false],
      [null, true],
      [{ apiBaseUrl: 42 }, true],
      [{}, true],
      [{ apiBaseUrl: 'not a url' }, true],
      [{ apiBaseUrl: 'ftp://api.example' }, true],
    ];
    for (const [body, ok] of bodies) {
      expect(await loadAppConfig(() => Promise.resolve(response(body, ok)), base)).toBeNull();
    }
    expect(await loadAppConfig(() => Promise.reject(new Error('offline')), base)).toBeNull();
  });

  it('accepts an explicit empty base URL as same origin', async () => {
    const config = await loadAppConfig(
      () => Promise.resolve(response({ apiBaseUrl: '' })),
      'http://x/',
    );
    expect(config).toEqual({ apiBaseUrl: '' });
  });
});

describe('AppConfig', () => {
  it('flags a failed load instead of silently using same origin', () => {
    const config = TestBed.inject(AppConfig);
    expect(config.loadFailed()).toBe(false);
    config.applyLoaded(null);
    expect(config.loadFailed()).toBe(true);
  });

  it('applies a loaded config', () => {
    const config = TestBed.inject(AppConfig);
    config.applyLoaded({ apiBaseUrl: 'https://api.example/' });
    expect(config.loadFailed()).toBe(false);
    expect(config.apiBaseUrl()).toBe('https://api.example');
  });

  it('normalises trailing slashes and whitespace in the base URL', () => {
    const config = TestBed.inject(AppConfig);
    config.set({ apiBaseUrl: ' https://api.example/// ' });
    expect(config.apiUrl('/api/me')).toBe('https://api.example/api/me');
  });

  it('stores the normalised base and builds API URLs', () => {
    const config = TestBed.inject(AppConfig);
    expect(config.apiUrl('/api/me')).toBe('/api/me');
    config.set({ apiBaseUrl: 'https://api.example/base/' });
    expect(config.apiBaseUrl()).toBe('https://api.example/base');
    expect(config.apiUrl('/health')).toBe('https://api.example/base/health');
  });
});

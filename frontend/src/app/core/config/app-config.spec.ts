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

  it('falls back to same origin on HTTP errors, bad JSON or network failure', async () => {
    const base = 'http://localhost:4200/';
    expect(await loadAppConfig(() => Promise.resolve(response(null, false)), base)).toEqual({
      apiBaseUrl: '',
    });
    expect(await loadAppConfig(() => Promise.resolve(response(null)), base)).toEqual({
      apiBaseUrl: '',
    });
    expect(await loadAppConfig(() => Promise.reject(new Error('offline')), base)).toEqual({
      apiBaseUrl: '',
    });
    expect(await loadAppConfig(() => Promise.resolve(response({ apiBaseUrl: 42 })), base)).toEqual({
      apiBaseUrl: '',
    });
  });
});

describe('AppConfig', () => {
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

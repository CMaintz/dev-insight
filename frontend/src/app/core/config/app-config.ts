import { Injectable, signal } from '@angular/core';

export interface AppConfigData {
  /** Absolute origin (+ optional path) of the API; empty string = same origin. */
  apiBaseUrl: string;
}

export const DEFAULT_CONFIG: AppConfigData = { apiBaseUrl: '' };

/** Drops trailing slashes so `${base}/api/...` never produces `//api`. */
export function normaliseBaseUrl(url: unknown): string {
  return typeof url === 'string' ? url.trim().replace(/\/+$/, '') : '';
}

/** Joins the API base with a root-relative API path (`/api/...`, `/health`). */
export function joinApiUrl(base: string, path: string): string {
  return `${normaliseBaseUrl(base)}${path}`;
}

type FetchFn = (input: string, init?: RequestInit) => Promise<Response>;

/**
 * Loads `config.json` relative to the document base href (so it works under the GitHub Pages
 * sub-path `/DevInsight/`). Any failure falls back to same-origin defaults.
 */
export async function loadAppConfig(
  fetchFn: FetchFn = (input, init) => fetch(input, init),
  baseUri: string = document.baseURI,
): Promise<AppConfigData> {
  try {
    const response = await fetchFn(new URL('config.json', baseUri).toString(), {
      cache: 'no-store',
    });
    if (!response.ok) {
      return DEFAULT_CONFIG;
    }
    const body = (await response.json()) as Partial<AppConfigData> | null;
    return { apiBaseUrl: normaliseBaseUrl(body?.apiBaseUrl) };
  } catch {
    return DEFAULT_CONFIG;
  }
}

/** Runtime configuration, populated by an app initializer before the first request. */
@Injectable({ providedIn: 'root' })
export class AppConfig {
  private readonly data = signal<AppConfigData>(DEFAULT_CONFIG);

  readonly apiBaseUrl = () => this.data().apiBaseUrl;

  set(data: AppConfigData): void {
    this.data.set({ apiBaseUrl: normaliseBaseUrl(data.apiBaseUrl) });
  }

  /** Absolute (or same-origin) URL for an API path such as `/api/auth/github/login`. */
  apiUrl(path: string): string {
    return joinApiUrl(this.apiBaseUrl(), path);
  }
}

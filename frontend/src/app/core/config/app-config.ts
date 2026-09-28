import { Injectable, signal } from '@angular/core';

export interface AppConfigData {
  apiBaseUrl: string;
}

const DEFAULT_CONFIG: AppConfigData = { apiBaseUrl: '' };

function normaliseBaseUrl(url: unknown): string {
  return typeof url === 'string' ? url.trim().replace(/\/+$/, '') : '';
}

function joinApiUrl(base: string, path: string): string {
  return `${normaliseBaseUrl(base)}${path}`;
}

type FetchFn = (input: string, init?: RequestInit) => Promise<Response>;

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

@Injectable({ providedIn: 'root' })
export class AppConfig {
  private readonly data = signal<AppConfigData>(DEFAULT_CONFIG);

  readonly apiBaseUrl = () => this.data().apiBaseUrl;

  set(data: AppConfigData): void {
    this.data.set({ apiBaseUrl: normaliseBaseUrl(data.apiBaseUrl) });
  }

  apiUrl(path: string): string {
    return joinApiUrl(this.apiBaseUrl(), path);
  }
}

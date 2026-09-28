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

function isUsableBaseUrl(value: string): boolean {
  if (value === '') {
    return true;
  }
  try {
    const url = new URL(value);
    return url.protocol === 'https:' || url.protocol === 'http:';
  } catch {
    return false;
  }
}

function parseConfig(body: unknown): AppConfigData | null {
  if (!body || typeof body !== 'object') {
    return null;
  }
  const apiBaseUrl = (body as Partial<Record<keyof AppConfigData, unknown>>).apiBaseUrl;
  if (typeof apiBaseUrl !== 'string') {
    return null;
  }
  const normalised = normaliseBaseUrl(apiBaseUrl);
  return isUsableBaseUrl(normalised) ? { apiBaseUrl: normalised } : null;
}

export async function loadAppConfig(
  fetchFn: FetchFn = (input, init) => fetch(input, init),
  baseUri: string = document.baseURI,
): Promise<AppConfigData | null> {
  try {
    const response = await fetchFn(new URL('config.json', baseUri).toString(), {
      cache: 'no-store',
    });
    return response.ok ? parseConfig(await response.json()) : null;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AppConfig {
  private readonly data = signal<AppConfigData>(DEFAULT_CONFIG);
  private readonly failed = signal(false);

  readonly loadFailed = this.failed.asReadonly();

  readonly apiBaseUrl = () => this.data().apiBaseUrl;

  set(data: AppConfigData): void {
    this.data.set({ apiBaseUrl: normaliseBaseUrl(data.apiBaseUrl) });
  }

  applyLoaded(data: AppConfigData | null): void {
    if (data) {
      this.set(data);
    } else {
      this.failed.set(true);
    }
  }

  apiUrl(path: string): string {
    return joinApiUrl(this.apiBaseUrl(), path);
  }
}

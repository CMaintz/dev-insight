import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthTokenStore } from '../auth/auth-token.store';
import { AppConfig } from '../config/app-config';
import { apiBaseUrlInterceptor, bearerTokenInterceptor, isApiPath } from './api.interceptors';

describe('API interceptors', () => {
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([bearerTokenInterceptor, apiBaseUrlInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('leaves URLs untouched and sends no Authorization without config or token', () => {
    http.get('/api/me').subscribe();
    const req = controller.expectOne('/api/me');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('prefixes /api and /health with the configured base URL', () => {
    TestBed.inject(AppConfig).set({ apiBaseUrl: 'https://api.example/' });
    http.get('/api/repos').subscribe();
    http.get('/health').subscribe();
    http.get('config.json').subscribe();
    http.get('https://elsewhere.example/x').subscribe();
    controller.expectOne('https://api.example/api/repos').flush([]);
    controller.expectOne('https://api.example/health').flush('Healthy');
    controller.expectOne('config.json').flush({});
    controller.expectOne('https://elsewhere.example/x').flush({});
  });

  it('attaches the bearer token to API requests only', () => {
    TestBed.inject(AuthTokenStore).set('tok-1');
    TestBed.inject(AppConfig).set({ apiBaseUrl: 'https://api.example' });
    http.get('/api/me').subscribe();
    http.get('https://images.example/a.png').subscribe();

    const api = controller.expectOne('https://api.example/api/me');
    expect(api.request.headers.get('Authorization')).toBe('Bearer tok-1');
    api.flush({});
    const other = controller.expectOne('https://images.example/a.png');
    expect(other.request.headers.has('Authorization')).toBe(false);
    other.flush({});
  });

  it('does not override an explicit Authorization header', () => {
    TestBed.inject(AuthTokenStore).set('tok-1');
    http.get('/api/me', { headers: { Authorization: 'Bearer other' } }).subscribe();
    const req = controller.expectOne('/api/me');
    expect(req.request.headers.get('Authorization')).toBe('Bearer other');
    req.flush({});
  });
});

describe('isApiPath', () => {
  it('matches API and health paths only', () => {
    expect(isApiPath('/api/x')).toBe(true);
    expect(isApiPath('/health')).toBe(true);
    expect(isApiPath('/health?full=1')).toBe(true);
    expect(isApiPath('/healthy')).toBe(false);
    expect(isApiPath('/apix')).toBe(false);
    expect(isApiPath('https://api.example/api/x')).toBe(false);
  });
});

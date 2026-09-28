import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthTokenStore } from '../auth/auth-token.store';
import { SessionStore } from '../auth/session.store';
import { ToastService } from '../notifications/toast.service';
import { SILENT_ERRORS, errorInterceptor, isPublicRequest } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let navigate: ReturnType<typeof vi.fn>;
  let markSignedOut: ReturnType<typeof vi.fn>;
  let toastError: ReturnType<typeof vi.fn>;
  let clearToken: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    navigate = vi.fn().mockResolvedValue(true);
    markSignedOut = vi.fn();
    toastError = vi.fn();
    clearToken = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigateByUrl: navigate } },
        { provide: SessionStore, useValue: { markSignedOut } },
        { provide: ToastService, useValue: { error: toastError } },
        { provide: AuthTokenStore, useValue: { clear: clearToken } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  function fail(url: string, status: number, body: object | null = null, context?: HttpContext) {
    const onError = vi.fn();
    http.get(url, { context }).subscribe({ error: onError });
    controller.expectOne(url).flush(body, { status, statusText: 'x' });
    return onError;
  }

  it('signs out and returns to the landing page on 401 from a private endpoint', () => {
    const onError = fail('/api/repos', 401);
    expect(markSignedOut).toHaveBeenCalled();
    expect(clearToken).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/');
    expect(toastError).not.toHaveBeenCalled();
    expect(onError).toHaveBeenCalled();
  });

  it('does not redirect for the session probe or the public portfolio', () => {
    fail('/api/me', 401);
    fail('/api/portfolio/octocat', 401);
    expect(navigate).not.toHaveBeenCalled();
    expect(markSignedOut).not.toHaveBeenCalled();
    expect(clearToken).not.toHaveBeenCalled();
  });

  it('leaves 404 to the page', () => {
    fail('/api/analysis/r1', 404);
    expect(toastError).not.toHaveBeenCalled();
  });

  it('toasts problem details for other errors', () => {
    fail('/api/projects', 400, { title: 'Invalid project', detail: 'Name is required' });
    expect(toastError).toHaveBeenCalledWith('Invalid project', 'Name is required');
  });

  it('stays silent when the request opts out', () => {
    fail('/api/projects', 500, null, new HttpContext().set(SILENT_ERRORS, true));
    expect(toastError).not.toHaveBeenCalled();
  });
});

describe('isPublicRequest', () => {
  it('matches exact and prefix paths, ignoring origin and query', () => {
    expect(isPublicRequest('/api/me')).toBe(true);
    expect(isPublicRequest('http://localhost:4200/api/portfolio/x?y=1')).toBe(true);
    expect(isPublicRequest('/api/me/profile')).toBe(false);
    expect(isPublicRequest('/api/dashboard')).toBe(false);
  });
});

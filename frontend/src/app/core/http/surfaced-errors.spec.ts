import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ToastService } from '../notifications/toast.service';
import { UserActionErrors } from './surfaced-errors';

describe('UserActionErrors', () => {
  let errors: UserActionErrors;
  let toastError: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    toastError = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: ToastService, useValue: { error: toastError } }],
    });
    errors = TestBed.inject(UserActionErrors);
  });

  const http = (status: number) => new HttpErrorResponse({ status });

  it('returns the value of a successful request', async () => {
    expect(await errors.resultOrNothing(of(42), 'gone')).toBe(42);
  });

  it('swallows errors the interceptor already toasted', async () => {
    expect(
      await errors.resultOrNothing(
        throwError(() => http(500)),
        'gone',
      ),
    ).toBeUndefined();
    expect(
      await errors.resultOrNothing(
        throwError(() => http(400)),
        'gone',
      ),
    ).toBeUndefined();
    expect(toastError).not.toHaveBeenCalled();
  });

  it('explains a 404 with the caller-specific message', async () => {
    expect(
      await errors.resultOrNothing(
        throwError(() => http(404)),
        'Repo is gone',
      ),
    ).toBeUndefined();
    expect(toastError).toHaveBeenCalledWith('Not found', 'Repo is gone');
  });

  it('rethrows 401 and non-HTTP errors', async () => {
    await expect(
      errors.resultOrNothing(
        throwError(() => http(401)),
        'gone',
      ),
    ).rejects.toBeInstanceOf(HttpErrorResponse);
    await expect(
      errors.resultOrNothing(
        throwError(() => new TypeError('bug')),
        'gone',
      ),
    ).rejects.toThrow('bug');
  });
});

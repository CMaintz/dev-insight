import { HttpErrorResponse } from '@angular/common/http';
import { describeHttpError, isNotFound } from './problem-details';

function error(status: number, body: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, url: '/api/x' });
}

describe('describeHttpError', () => {
  it('uses the problem-details title and detail', () => {
    expect(describeHttpError(error(400, { title: 'Invalid', detail: 'Name is required' }))).toEqual(
      { title: 'Invalid', detail: 'Name is required' },
    );
  });

  it('falls back to a status title when the problem has only a detail', () => {
    expect(describeHttpError(error(412, { detail: 'No token' }))).toEqual({
      title: 'GitHub access is missing — please sign in again',
      detail: 'No token',
    });
  });

  it('explains network failures', () => {
    expect(describeHttpError(error(0))).toEqual({
      title: 'Cannot reach the DevInsight API',
      detail: 'Is the backend running?',
    });
  });

  it('uses a generic server message for 5xx without a body', () => {
    expect(describeHttpError(error(503, 'oops')).title).toBe('The server had a problem');
  });

  it('uses a generic message for unknown client errors', () => {
    expect(describeHttpError(error(418)).title).toBe('Something went wrong');
  });
});

describe('isNotFound', () => {
  it('detects 404 responses only', () => {
    expect(isNotFound(error(404))).toBe(true);
    expect(isNotFound(error(500))).toBe(false);
    expect(isNotFound(new Error('x'))).toBe(false);
    expect(isNotFound(undefined)).toBe(false);
  });
});

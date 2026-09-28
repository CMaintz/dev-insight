import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/api.models';

export interface ErrorMessage {
  title: string;
  detail?: string;
}

function asProblem(body: unknown): ProblemDetails | null {
  if (body && typeof body === 'object') {
    const candidate = body as ProblemDetails;
    if (typeof candidate.title === 'string' || typeof candidate.detail === 'string') {
      return candidate;
    }
  }
  return null;
}

const FALLBACK_TITLES: Record<number, string> = {
  0: 'Cannot reach the DevInsight API',
  400: 'The request was rejected',
  403: 'You are not allowed to do that',
  404: 'Not found',
  412: 'GitHub access is missing — please sign in again',
  429: 'Too many requests — try again shortly',
};

function fallbackTitle(status: number): string {
  return (
    FALLBACK_TITLES[status] ?? (status >= 500 ? 'The server had a problem' : 'Something went wrong')
  );
}

export function describeHttpError(error: HttpErrorResponse): ErrorMessage {
  const problem = asProblem(error.error);
  const fallback = fallbackTitle(error.status);
  if (problem) {
    return { title: problem.title ?? fallback, detail: problem.detail ?? undefined };
  }
  return { title: fallback, detail: error.status === 0 ? 'Is the backend running?' : undefined };
}

export function isNotFound(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 404;
}

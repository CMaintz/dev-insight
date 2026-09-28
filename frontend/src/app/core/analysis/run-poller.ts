import { Injectable, InjectionToken, inject } from '@angular/core';
import { Observable, exhaustMap, take, takeWhile, timer } from 'rxjs';
import { AnalysisApi } from '../api/analysis.api';
import { AnalysisRun, RunStatus } from '../models/api.models';

export const RUN_POLL_INTERVAL_MS = new InjectionToken<number>('RUN_POLL_INTERVAL_MS', {
  providedIn: 'root',
  factory: () => 2000,
});

/** Upper bound on polls per run (~10 minutes at the default interval). */
export const RUN_POLL_MAX_ATTEMPTS = new InjectionToken<number>('RUN_POLL_MAX_ATTEMPTS', {
  providedIn: 'root',
  factory: () => 300,
});

export function isTerminal(status: RunStatus): boolean {
  return status === 'succeeded' || status === 'failed';
}

/** Polls `GET /api/analysis/runs/{id}` until the run succeeds or fails (inclusive). */
@Injectable({ providedIn: 'root' })
export class RunPoller {
  private readonly api = inject(AnalysisApi);
  private readonly intervalMs = inject(RUN_POLL_INTERVAL_MS);
  private readonly maxAttempts = inject(RUN_POLL_MAX_ATTEMPTS);

  poll(runId: string): Observable<AnalysisRun> {
    return timer(this.intervalMs, this.intervalMs).pipe(
      take(this.maxAttempts),
      exhaustMap(() => this.api.getRun(runId)),
      takeWhile((run) => !isTerminal(run.status), true),
    );
  }
}

import { Injectable, inject } from '@angular/core';
import { Observable, exhaustMap, take, takeWhile, timer } from 'rxjs';
import { AnalysisApi } from '../api/analysis.api';
import { AnalysisRun, RunStatus } from '../models/api.models';

const POLL_INTERVAL_MS = 2000;
const MAX_POLLS_PER_RUN = 300;

export function isTerminal(status: RunStatus): boolean {
  return status === 'succeeded' || status === 'failed';
}

@Injectable({ providedIn: 'root' })
export class RunPoller {
  private readonly api = inject(AnalysisApi);

  poll(runId: string): Observable<AnalysisRun> {
    return timer(POLL_INTERVAL_MS, POLL_INTERVAL_MS).pipe(
      take(MAX_POLLS_PER_RUN),
      exhaustMap(() => this.api.getRun(runId)),
      takeWhile((run) => !isTerminal(run.status), true),
    );
  }
}

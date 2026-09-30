import { Injectable, computed, inject, signal } from '@angular/core';
import { Observer, Subscription, firstValueFrom } from 'rxjs';
import { AnalysisApi } from '../api/analysis.api';
import { AnalysisRun, ScopeParam } from '../models/api.models';
import { ToastService } from '../notifications/toast.service';
import { RunPoller, isTerminal } from './run-poller';

type RunPhase = 'idle' | 'active' | 'succeeded' | 'failed' | 'timedOut';

export interface RepoRunState {
  phase: RunPhase;
  label: string;
  error?: string;
}

interface TrackedRun extends AnalysisRun {
  lostTrack?: boolean;
}

const IDLE: RepoRunState = { phase: 'idle', label: '' };
const LOST_TRACK_MESSAGE = 'We lost track of this run — refresh later to see its result.';

function summariseRuns(runs: readonly TrackedRun[]): RepoRunState {
  if (runs.length === 0) {
    return IDLE;
  }
  const pending = runs.filter((run) => !isTerminal(run.status));
  if (pending.length > 0) {
    const running = pending.some((run) => run.status === 'running');
    const done = runs.length - pending.length;
    const label = running ? 'Analysing' : 'Queued';
    return {
      phase: 'active',
      label: runs.length > 1 ? `${label} (${done}/${runs.length})` : label,
    };
  }
  if (runs.some((run) => run.lostTrack)) {
    return { phase: 'timedOut', label: 'Timed out', error: LOST_TRACK_MESSAGE };
  }
  const failed = runs.find((run) => run.status === 'failed');
  if (failed) {
    return { phase: 'failed', label: 'Failed', error: failed.error ?? 'Analysis failed' };
  }
  return { phase: 'succeeded', label: 'Analysed' };
}

const ALL_SCOPES: readonly ScopeParam[] = ['repo', 'user'];

@Injectable({ providedIn: 'root' })
export class RunTracker {
  private readonly api = inject(AnalysisApi);
  private readonly poller = inject(RunPoller);
  private readonly toasts = inject(ToastService);

  private readonly runs = signal<Record<string, TrackedRun>>({});
  private readonly polls = new Map<string, Subscription>();

  readonly settledCount = signal(0);
  readonly activeCount = computed(
    () => Object.values(this.runs()).filter((run) => !isTerminal(run.status)).length,
  );

  stateFor(repositoryId: string): RepoRunState {
    return summariseRuns(
      Object.values(this.runs()).filter((run) => run.repositoryId === repositoryId),
    );
  }

  isActive(repositoryId: string): boolean {
    return this.stateFor(repositoryId).phase === 'active';
  }

  async analyse(repositoryId: string, scopes: readonly ScopeParam[] = ALL_SCOPES): Promise<void> {
    this.forget(repositoryId);
    const outcomes = await Promise.allSettled(
      scopes.map((scope) => firstValueFrom(this.api.run(repositoryId, scope))),
    );
    for (const outcome of outcomes) {
      if (outcome.status === 'fulfilled') {
        this.track(outcome.value);
      }
    }
    const firstFailure = outcomes.find((outcome) => outcome.status === 'rejected');
    if (firstFailure) {
      throw firstFailure.reason;
    }
  }

  async analyseAll(): Promise<number> {
    const started = await firstValueFrom(this.api.runAll());
    started.forEach((run) => this.track(run));
    return started.length;
  }

  stopAll(): void {
    this.polls.forEach((subscription) => subscription.unsubscribe());
    this.polls.clear();
    this.runs.set({});
  }

  private forget(repositoryId: string): void {
    const forgotten = Object.values(this.runs()).filter((run) => run.repositoryId === repositoryId);
    forgotten.forEach((run) => this.stopPolling(run.id));
    this.runs.update((all) =>
      Object.fromEntries(
        Object.entries(all).filter(([, run]) => run.repositoryId !== repositoryId),
      ),
    );
  }

  private stopPolling(runId: string): void {
    this.polls.get(runId)?.unsubscribe();
    this.polls.delete(runId);
  }

  private upsert(run: TrackedRun): void {
    this.runs.update((all) => ({ ...all, [run.id]: run }));
    if (!isTerminal(run.status)) {
      return;
    }
    this.settledCount.update((n) => n + 1);
    if (run.lostTrack) {
      this.toasts.error('Lost track of an analysis run', LOST_TRACK_MESSAGE);
    } else if (run.status === 'failed') {
      this.toasts.error('An analysis run failed', run.error ?? undefined);
    }
  }

  private markLostTrack(run: AnalysisRun): void {
    this.upsert({ ...run, status: 'failed', error: LOST_TRACK_MESSAGE, lostTrack: true });
  }

  private track(run: AnalysisRun): void {
    this.upsert(run);
    if (isTerminal(run.status)) {
      return;
    }
    const subscription = this.poller.poll(run.id).subscribe(this.observeUntilSettled(run));
    if (!subscription.closed) {
      this.polls.set(run.id, subscription);
    }
  }

  private observeUntilSettled(run: AnalysisRun): Partial<Observer<AnalysisRun>> {
    let latest = run;
    const stopTracking = () => {
      this.polls.delete(run.id);
      if (!isTerminal(latest.status)) {
        this.markLostTrack(latest);
      }
    };
    return {
      next: (update) => {
        latest = update;
        this.upsert(update);
      },
      error: stopTracking,
      complete: stopTracking,
    };
  }
}

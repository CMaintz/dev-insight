import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AnalysisApi } from '../api/analysis.api';
import { AnalysisRun, ScopeParam } from '../models/api.models';
import { ToastService } from '../notifications/toast.service';
import { RunPoller, isTerminal } from './run-poller';

export type RunPhase = 'idle' | 'active' | 'succeeded' | 'failed';

export interface RepoRunState {
  phase: RunPhase;
  label: string;
  error?: string;
}

const IDLE: RepoRunState = { phase: 'idle', label: '' };

/** Collapses the runs of one repository (one per scope) into a single UI state. */
export function summariseRuns(runs: readonly AnalysisRun[]): RepoRunState {
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
  const failed = runs.find((run) => run.status === 'failed');
  if (failed) {
    return { phase: 'failed', label: 'Failed', error: failed.error ?? 'Analysis failed' };
  }
  return { phase: 'succeeded', label: 'Analysed' };
}

const ALL_SCOPES: readonly ScopeParam[] = ['repo', 'user'];

/**
 * App-wide registry of analysis runs started from the UI. Polls each run to completion
 * (keeps polling across navigation) and bumps `settledCount` whenever one finishes so
 * pages can refetch their data.
 */
@Injectable({ providedIn: 'root' })
export class RunTracker {
  private readonly api = inject(AnalysisApi);
  private readonly poller = inject(RunPoller);
  private readonly toasts = inject(ToastService);

  private readonly runs = signal<Record<string, AnalysisRun>>({});

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

  /** Analyses one repository in both scopes so the scope toggle has data either way. */
  async analyse(repositoryId: string, scopes: readonly ScopeParam[] = ALL_SCOPES): Promise<void> {
    this.forget(repositoryId);
    const started = await Promise.all(
      scopes.map((scope) => firstValueFrom(this.api.run(repositoryId, scope))),
    );
    started.forEach((run) => this.track(run));
  }

  /** Analyses every selected repository; returns how many runs were queued. */
  async analyseAll(): Promise<number> {
    const started = await firstValueFrom(this.api.runAll());
    started.forEach((run) => this.track(run));
    return started.length;
  }

  private forget(repositoryId: string): void {
    this.runs.update((all) =>
      Object.fromEntries(
        Object.entries(all).filter(([, run]) => run.repositoryId !== repositoryId),
      ),
    );
  }

  private upsert(run: AnalysisRun): void {
    this.runs.update((all) => ({ ...all, [run.id]: run }));
    if (isTerminal(run.status)) {
      this.settledCount.update((n) => n + 1);
      if (run.status === 'failed') {
        this.toasts.error('An analysis run failed', run.error ?? undefined);
      }
    }
  }

  private track(run: AnalysisRun): void {
    this.upsert(run);
    if (isTerminal(run.status)) {
      return;
    }
    this.poller.poll(run.id).subscribe({
      next: (update) => this.upsert(update),
      error: () =>
        this.upsert({ ...run, status: 'failed', error: 'Lost track of this run — refresh later' }),
    });
  }
}

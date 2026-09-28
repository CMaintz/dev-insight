import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ReposApi } from '../api/repos.api';
import { ToastService } from '../notifications/toast.service';
import { RunTracker } from './run-tracker';

/** Workspace-wide commands shared by the dashboard and repositories pages. */
@Injectable({ providedIn: 'root' })
export class WorkspaceActions {
  private readonly repos = inject(ReposApi);
  private readonly tracker = inject(RunTracker);
  private readonly toasts = inject(ToastService);

  readonly importing = signal(false);
  readonly startingAnalysis = signal(false);
  /** Bumped after every successful import so pages can refetch. */
  readonly importVersion = signal(0);

  async importFromGitHub(): Promise<void> {
    if (this.importing()) {
      return;
    }
    this.importing.set(true);
    try {
      const result = await firstValueFrom(this.repos.importFromGitHub());
      this.toasts.success(
        'Repositories imported',
        `${result.imported} new, ${result.updated} updated — ${result.total} in total.`,
      );
      this.importVersion.update((n) => n + 1);
    } catch {
      // The error interceptor already surfaced the problem.
    } finally {
      this.importing.set(false);
    }
  }

  async analyseAllSelected(): Promise<void> {
    if (this.startingAnalysis()) {
      return;
    }
    this.startingAnalysis.set(true);
    try {
      const queued = await this.tracker.analyseAll();
      if (queued === 0) {
        this.toasts.info('Nothing to analyse', 'Select at least one repository first.');
      } else {
        this.toasts.info(
          'Analysis started',
          `${queued} runs queued. Results appear as they finish.`,
        );
      }
    } catch {
      // Surfaced by the interceptor.
    } finally {
      this.startingAnalysis.set(false);
    }
  }
}

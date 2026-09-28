import { Injectable, inject, signal } from '@angular/core';
import { ReposApi } from '../api/repos.api';
import { errorAlreadyShownByInterceptor, resultOrNothing } from '../http/surfaced-errors';
import { ToastService } from '../notifications/toast.service';
import { RunTracker } from './run-tracker';

@Injectable({ providedIn: 'root' })
export class WorkspaceActions {
  private readonly repos = inject(ReposApi);
  private readonly tracker = inject(RunTracker);
  private readonly toasts = inject(ToastService);

  readonly importing = signal(false);
  readonly startingAnalysis = signal(false);
  readonly importVersion = signal(0);

  async importFromGitHub(): Promise<void> {
    if (this.importing()) {
      return;
    }
    this.importing.set(true);
    const result = await resultOrNothing(this.repos.importFromGitHub());
    this.importing.set(false);
    if (result) {
      this.toasts.success(
        'Repositories imported',
        `${result.imported} new, ${result.updated} updated — ${result.total} in total.`,
      );
      this.importVersion.update((n) => n + 1);
    }
  }

  async analyseAllSelected(): Promise<void> {
    if (this.startingAnalysis()) {
      return;
    }
    this.startingAnalysis.set(true);
    const queued = await this.tracker.analyseAll().catch(errorAlreadyShownByInterceptor);
    this.startingAnalysis.set(false);
    if (queued === undefined) {
      return;
    }
    if (queued === 0) {
      this.toasts.info('Nothing to analyse', 'Select at least one repository first.');
    } else {
      this.toasts.info('Analysis started', `${queued} runs queued. Results appear as they finish.`);
    }
  }
}

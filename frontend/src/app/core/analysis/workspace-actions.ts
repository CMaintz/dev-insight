import { Injectable, inject, signal } from '@angular/core';
import { BusyFlag } from '../../shared/util/busy-flag';
import { ReposApi } from '../api/repos.api';
import { UserActionErrors } from '../http/surfaced-errors';
import { ToastService } from '../notifications/toast.service';
import { RunTracker } from './run-tracker';

const REPOSITORY_GONE = 'That repository no longer exists — re-import from GitHub.';

@Injectable({ providedIn: 'root' })
export class WorkspaceActions {
  private readonly repos = inject(ReposApi);
  private readonly tracker = inject(RunTracker);
  private readonly toasts = inject(ToastService);
  private readonly errors = inject(UserActionErrors);

  private readonly importFlag = new BusyFlag();
  private readonly analyseAllFlag = new BusyFlag();

  readonly importing = this.importFlag.active;
  readonly startingAnalysis = this.analyseAllFlag.active;
  readonly importVersion = signal(0);

  async importFromGitHub(): Promise<void> {
    const result = await this.importFlag.run(() =>
      this.errors.resultOrNothing(
        this.repos.importFromGitHub(),
        'The import endpoint was not found — is the API up to date?',
      ),
    );
    if (result) {
      this.toasts.success(
        'Repositories imported',
        `${result.imported} new, ${result.updated} updated — ${result.total} in total.`,
      );
      this.importVersion.update((n) => n + 1);
    }
  }

  async analyseRepository(repositoryId: string): Promise<void> {
    await this.tracker.analyse(repositoryId).catch(this.handleMissingRepository);
  }

  private readonly handleMissingRepository = (error: unknown): undefined =>
    this.errors.handle(error, REPOSITORY_GONE);

  async analyseAllSelected(): Promise<void> {
    const queued = await this.analyseAllFlag.run(() =>
      this.tracker.analyseAll().catch(this.handleMissingRepository),
    );
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

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RunTracker } from '../../core/analysis/run-tracker';
import { WorkspaceActions } from '../../core/analysis/workspace-actions';
import { ReposApi } from '../../core/api/repos.api';
import { UserActionErrors } from '../../core/http/surfaced-errors';
import { Repository } from '../../core/models/api.models';
import { EmptyState } from '../../shared/ui/empty-state';
import { WorkspaceActionsBar } from '../../shared/ui/workspace-actions-bar';
import { reloadOn } from '../../shared/util/reload-on';
import {
  EMPTY_FILTER,
  RepoFilter,
  distinctLanguages,
  filterRepositories,
  sortByActivity,
} from './repo-filter';
import { RepoRow } from './repo-row';

@Component({
  selector: 'app-repositories-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [WorkspaceActionsBar, EmptyState, RepoRow],
  templateUrl: './repositories.page.html',
  styleUrl: './repositories.page.scss',
})
export class RepositoriesPage {
  private readonly api = inject(ReposApi);
  protected readonly tracker = inject(RunTracker);
  private readonly errors = inject(UserActionErrors);
  protected readonly actions = inject(WorkspaceActions);

  protected readonly repos = this.api.listResource();
  protected readonly filter = signal<RepoFilter>(EMPTY_FILTER);
  protected readonly saving = signal<ReadonlySet<string>>(new Set());

  private readonly all = computed(() => (this.repos.hasValue() ? this.repos.value() : []));
  protected readonly languages = computed(() => distinctLanguages(this.all()));
  protected readonly visible = computed(() =>
    sortByActivity(filterRepositories(this.all(), this.filter())),
  );
  protected readonly selectedCount = computed(() => this.all().filter((r) => r.isSelected).length);

  constructor() {
    reloadOn(this.actions.importVersion, this.repos);
  }

  protected setFilter(patch: Partial<RepoFilter>): void {
    this.filter.update((f) => ({ ...f, ...patch }));
  }

  protected inputValue(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }

  protected checkedValue(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }

  protected async toggleSelected(repo: Repository, isSelected: boolean): Promise<void> {
    this.markSaving(repo.id, true);
    this.replace({ ...repo, isSelected });
    try {
      this.replace(await firstValueFrom(this.api.setSelected(repo.id, isSelected)));
    } catch {
      this.replace(repo);
    } finally {
      this.markSaving(repo.id, false);
    }
  }

  protected async analyse(repo: Repository): Promise<void> {
    await this.tracker
      .analyse(repo.id)
      .catch((error: unknown) =>
        this.errors.handle(error, 'That repository no longer exists — re-import from GitHub.'),
      );
  }

  private replace(updated: Repository): void {
    this.repos.update((list) => (list ?? []).map((r) => (r.id === updated.id ? updated : r)));
  }

  private markSaving(id: string, on: boolean): void {
    this.saving.update((set) => {
      const next = new Set(set);
      if (on) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }
}

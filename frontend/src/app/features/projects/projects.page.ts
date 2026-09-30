import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { map } from 'rxjs';
import { ProjectsApi } from '../../core/api/projects.api';
import { ReposApi } from '../../core/api/repos.api';
import { UserActionErrors } from '../../core/http/surfaced-errors';
import { Project, ProjectInput } from '../../core/models/api.models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyState } from '../../shared/ui/empty-state';
import { BusyFlag } from '../../shared/util/busy-flag';
import { listOrEmpty } from '../../shared/util/reload-on';
import { LoadError } from '../../shared/ui/load-error';
import { ProjectForm } from './project-form';
import { nextSortOrder } from './project-form.model';

const PROJECT_GONE = 'That project no longer exists — refresh the page.';

interface Editing {
  project: Project | null;
}

@Component({
  selector: 'app-projects-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoadError, EmptyState, ProjectForm],
  templateUrl: './projects.page.html',
  styleUrl: './projects.page.scss',
})
export class ProjectsPage {
  private readonly api = inject(ProjectsApi);
  private readonly toasts = inject(ToastService);
  private readonly errors = inject(UserActionErrors);

  protected readonly projects = this.api.listResource();
  protected readonly repos = inject(ReposApi).listResource();

  protected readonly editing = signal<Editing | null>(null);
  protected readonly editTargets = computed(() => {
    const editing = this.editing();
    return editing ? [{ key: editing.project?.id ?? 'new', project: editing.project }] : [];
  });
  private readonly saveFlag = new BusyFlag();
  protected readonly saving = this.saveFlag.active;
  protected readonly confirmingDelete = signal<string | null>(null);

  protected readonly sorted = computed(() =>
    [...this.projectList()].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name),
    ),
  );
  protected readonly repoList = listOrEmpty(this.repos);
  private readonly projectList = listOrEmpty(this.projects);
  protected readonly repoNames = computed(
    () => new Map(this.repoList().map((repo) => [repo.id, repo.name])),
  );
  protected readonly nextSort = computed(() => nextSortOrder(this.sorted()));

  protected startCreate(): void {
    this.editing.set({ project: null });
  }

  protected startEdit(project: Project): void {
    this.editing.set({ project });
  }

  protected async save(body: ProjectInput): Promise<void> {
    const current = this.editing();
    if (!current) {
      return;
    }
    const saved = await this.saveFlag.run(() =>
      this.errors.resultOrNothing(
        current.project ? this.api.update(current.project.id, body) : this.api.create(body),
        PROJECT_GONE,
      ),
    );
    if (!saved) {
      return;
    }
    this.projects.update((list) => [...(list ?? []).filter((p) => p.id !== saved.id), saved]);
    this.toasts.success(current.project ? 'Project updated' : 'Project created', saved.name);
    this.editing.set(null);
  }

  protected async remove(project: Project): Promise<void> {
    if (this.confirmingDelete() !== project.id) {
      this.confirmingDelete.set(project.id);
      return;
    }
    this.confirmingDelete.set(null);
    const deleted = await this.errors.resultOrNothing(
      this.api.delete(project.id).pipe(map(() => true)),
      PROJECT_GONE,
    );
    if (deleted) {
      this.projects.update((list) => (list ?? []).filter((p) => p.id !== project.id));
      this.toasts.success('Project deleted', project.name);
    }
  }

  protected linkedNames(project: Project): string[] {
    const names = this.repoNames();
    return project.linkedRepositoryIds.map((id) => names.get(id) ?? 'Unknown repository');
  }
}

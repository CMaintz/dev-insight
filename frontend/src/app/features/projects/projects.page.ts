import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { map } from 'rxjs';
import { ProjectsApi } from '../../core/api/projects.api';
import { ReposApi } from '../../core/api/repos.api';
import { UserActionErrors } from '../../core/http/surfaced-errors';
import { Project, ProjectInput } from '../../core/models/api.models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyState } from '../../shared/ui/empty-state';
import { ProjectForm } from './project-form';
import { nextSortOrder } from './project-form.model';

const PROJECT_GONE = 'That project no longer exists — refresh the page.';

interface Editing {
  project: Project | null;
}

@Component({
  selector: 'app-projects-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, ProjectForm],
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
  protected readonly saving = signal(false);
  protected readonly confirmingDelete = signal<string | null>(null);

  protected readonly sorted = computed(() =>
    [...(this.projects.hasValue() ? this.projects.value() : [])].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name),
    ),
  );
  protected readonly repoList = computed(() => (this.repos.hasValue() ? this.repos.value() : []));
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
    this.saving.set(true);
    const saved = await this.errors.resultOrNothing(
      current.project ? this.api.update(current.project.id, body) : this.api.create(body),
      PROJECT_GONE,
    );
    this.saving.set(false);
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

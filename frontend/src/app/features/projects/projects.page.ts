import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProjectsApi } from '../../core/api/projects.api';
import { ReposApi } from '../../core/api/repos.api';
import { Project, ProjectInput } from '../../core/models/api.models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyState } from '../../shared/ui/empty-state';
import { ProjectForm } from './project-form';
import { nextSortOrder } from './project-form.model';

/** `null` project = creating a new one. */
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

  protected readonly projects = this.api.listResource();
  protected readonly repos = inject(ReposApi).listResource();

  protected readonly editing = signal<Editing | null>(null);
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
    try {
      const saved = current.project
        ? await firstValueFrom(this.api.update(current.project.id, body))
        : await firstValueFrom(this.api.create(body));
      this.projects.update((list) => [...(list ?? []).filter((p) => p.id !== saved.id), saved]);
      this.toasts.success(current.project ? 'Project updated' : 'Project created', saved.name);
      this.editing.set(null);
    } catch {
      // Validation problems are surfaced by the interceptor toast; keep the form open.
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(project: Project): Promise<void> {
    if (this.confirmingDelete() !== project.id) {
      this.confirmingDelete.set(project.id);
      return;
    }
    this.confirmingDelete.set(null);
    try {
      await firstValueFrom(this.api.delete(project.id));
      this.projects.update((list) => (list ?? []).filter((p) => p.id !== project.id));
      this.toasts.success('Project deleted', project.name);
    } catch {
      // Surfaced by the interceptor.
    }
  }

  protected linkedNames(project: Project): string[] {
    const names = this.repoNames();
    return project.linkedRepositoryIds.map((id) => names.get(id) ?? 'Unknown repository');
  }
}

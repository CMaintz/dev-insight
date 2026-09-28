import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RepoRunState } from '../../core/analysis/run-tracker';
import { Repository } from '../../core/models/api.models';
import { formatCompact, formatRelative } from '../../shared/format/format';

@Component({
  selector: 'app-repo-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './repo-row.html',
  styleUrl: './repo-row.scss',
})
export class RepoRow {
  readonly repo = input.required<Repository>();
  readonly run = input.required<RepoRunState>();
  readonly saving = input(false);

  readonly selectedChange = output<boolean>();
  readonly analyse = output<void>();

  protected readonly checkboxId = computed(() => `select-${this.repo().id}`);
  protected readonly stars = computed(() => formatCompact(this.repo().stars));
  protected readonly forks = computed(() => formatCompact(this.repo().forks));
  protected readonly lastActivity = computed(() => formatRelative(this.repo().lastActivity));

  protected onToggle(event: Event): void {
    this.selectedChange.emit((event.target as HTMLInputElement).checked);
  }
}

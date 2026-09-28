import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RunTracker } from '../../core/analysis/run-tracker';
import { WorkspaceActions } from '../../core/analysis/workspace-actions';

@Component({
  selector: 'app-workspace-actions-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="btn"
      (click)="actions.importFromGitHub()"
      [disabled]="actions.importing()"
    >
      {{ actions.importing() ? 'Importing…' : 'Import from GitHub' }}
    </button>
    <button
      type="button"
      class="btn btn--primary"
      (click)="actions.analyseAllSelected()"
      [disabled]="analyseDisabled()"
    >
      {{ analysing() ? 'Analysing…' : 'Analyse all selected (' + selectedCount() + ')' }}
    </button>
  `,
  styles: ':host { display: contents; }',
})
export class WorkspaceActionsBar {
  protected readonly actions = inject(WorkspaceActions);
  private readonly tracker = inject(RunTracker);

  readonly selectedCount = input.required<number>();

  protected readonly analysing = computed(() => this.tracker.activeCount() > 0);
  protected readonly analyseDisabled = computed(
    () => this.actions.startingAnalysis() || this.analysing() || this.selectedCount() === 0,
  );
}

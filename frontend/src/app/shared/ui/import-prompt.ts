import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { WorkspaceActions } from '../../core/analysis/workspace-actions';
import { EmptyState } from './empty-state';

@Component({
  selector: 'app-import-prompt',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState],
  template: `
    <app-empty-state icon="📦" [heading]="heading()" [text]="text()">
      <button
        type="button"
        class="btn btn--primary"
        (click)="actions.importFromGitHub()"
        [disabled]="actions.importing()"
      >
        Import from GitHub
      </button>
    </app-empty-state>
  `,
})
export class ImportPrompt {
  protected readonly actions = inject(WorkspaceActions);

  readonly heading = input.required<string>();
  readonly text = input.required<string>();
}

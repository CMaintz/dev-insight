import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { EmptyState } from './empty-state';

@Component({
  selector: 'app-load-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState],
  template: `
    <app-empty-state icon="⚠" [heading]="heading()" [text]="text()">
      <button type="button" class="btn" (click)="retry.emit()">Retry</button>
    </app-empty-state>
  `,
})
export class LoadError {
  readonly heading = input.required<string>();
  readonly text = input('Try again in a moment.');
  readonly retry = output<void>();
}

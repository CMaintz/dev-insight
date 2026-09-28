import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-tile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tile">
      <span class="tile__label">{{ label() }}</span>
      <span class="tile__value">{{ value() }}</span>
      @if (hint()) {
        <span class="tile__hint">{{ hint() }}</span>
      }
      <ng-content />
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
    .tile {
      display: grid;
      gap: 0.25rem;
      height: 100%;
      padding: 1rem 1.25rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      background: var(--surface);
    }
    .tile__label {
      font-size: 0.8125rem;
      color: var(--text-secondary);
    }
    .tile__value {
      font-size: 1.75rem;
      font-weight: 650;
      line-height: 1.2;
    }
    .tile__hint {
      font-size: 0.75rem;
      color: var(--text-muted);
    }
  `,
})
export class KpiTile {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly hint = input('');
}

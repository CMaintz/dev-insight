import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { ScopeParam } from '../../core/models/api.models';
import { SCOPE_LABELS } from '../../core/scope/scope-preference';

let nextId = 0;

@Component({
  selector: 'app-scope-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <fieldset class="segmented">
      <legend class="visually-hidden">Analysis scope</legend>
      @for (option of options; track option.value) {
        <label class="segmented__option" [class.is-active]="scope() === option.value">
          <input
            type="radio"
            [name]="name"
            [value]="option.value"
            [checked]="scope() === option.value"
            (change)="scope.set(option.value)"
          />
          {{ option.label }}
        </label>
      }
    </fieldset>
  `,
  styles: `
    .segmented {
      display: inline-flex;
      margin: 0;
      padding: 0.25rem;
      border: 1px solid var(--border);
      border-radius: 999px;
      background: var(--surface-sunken);
      gap: 0.25rem;
    }
    .segmented__option {
      position: relative;
      padding: 0.375rem 0.875rem;
      border-radius: 999px;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-secondary);
      cursor: pointer;
      white-space: nowrap;
    }
    .segmented__option.is-active {
      background: var(--surface-raised);
      color: var(--text-primary);
      box-shadow: var(--shadow-sm);
    }
    .segmented__option:has(input:focus-visible) {
      outline: 2px solid var(--focus-ring);
      outline-offset: 2px;
    }
    input {
      position: absolute;
      opacity: 0;
      pointer-events: none;
    }
  `,
})
export class ScopeToggle {
  readonly scope = model<ScopeParam>('repo');

  protected readonly name = `scope-${nextId++}`;
  protected readonly options = (Object.keys(SCOPE_LABELS) as ScopeParam[]).map((value) => ({
    value,
    label: SCOPE_LABELS[value],
  }));
}

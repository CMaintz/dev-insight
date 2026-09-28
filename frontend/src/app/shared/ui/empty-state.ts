import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Friendly placeholder with a heading, guidance text and projected actions. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="empty" [attr.aria-label]="heading()">
      @if (icon()) {
        <div class="empty__icon" aria-hidden="true">{{ icon() }}</div>
      }
      <h2 class="empty__title">{{ heading() }}</h2>
      <p class="empty__text">{{ text() }}</p>
      <div class="empty__actions"><ng-content /></div>
    </section>
  `,
  styles: `
    .empty {
      display: grid;
      justify-items: center;
      gap: 0.5rem;
      padding: 2.5rem 1.5rem;
      text-align: center;
      border: 1px dashed var(--border-strong);
      border-radius: var(--radius-lg);
      background: var(--surface);
    }
    .empty__icon {
      font-size: 2rem;
      line-height: 1;
    }
    .empty__title {
      margin: 0;
      font-size: 1.125rem;
    }
    .empty__text {
      margin: 0;
      max-width: 36rem;
      color: var(--text-secondary);
    }
    .empty__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      justify-content: center;
      margin-top: 0.5rem;
    }
    .empty__actions:empty {
      display: none;
    }
  `,
})
export class EmptyState {
  readonly heading = input.required<string>();
  readonly text = input('');
  readonly icon = input('');
}

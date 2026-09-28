import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { scoreBand } from '../format/format';

/** Compact score pill for tables and cards: coloured dot + number (+ optional band label). */
@Component({
  selector: 'app-score-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (score() === null) {
      <span class="badge badge--none" [attr.title]="'Not analysed yet'">
        <span class="visually-hidden">{{ label() }}: </span>—
      </span>
    } @else {
      <span [class]="'badge tone-' + band()?.tone" [attr.title]="band()?.label">
        <span class="badge__dot" aria-hidden="true"></span>
        <span class="visually-hidden">{{ label() }}: </span>{{ score() }}
        @if (showBand()) {
          <span class="badge__band">{{ band()?.label }}</span>
        } @else {
          <span class="visually-hidden">, {{ band()?.label }}</span>
        }
      </span>
    }
  `,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
      background: var(--surface-sunken);
      font-weight: 600;
      font-variant-numeric: tabular-nums;
      font-size: 0.8125rem;
      white-space: nowrap;
    }
    .badge--none {
      color: var(--text-muted);
    }
    .badge__dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background: var(--tone-color);
    }
    .badge__band {
      font-weight: 400;
      color: var(--text-secondary);
    }
    .tone-good {
      --tone-color: var(--status-good);
    }
    .tone-fair {
      --tone-color: var(--status-warning);
    }
    .tone-weak {
      --tone-color: var(--status-critical);
    }
  `,
})
export class ScoreBadge {
  readonly score = input<number | null | undefined>(null);
  readonly label = input('Score');
  readonly showBand = input(false);

  protected readonly band = computed(() => {
    const score = this.score();
    return score === null || score === undefined ? null : scoreBand(score);
  });
}

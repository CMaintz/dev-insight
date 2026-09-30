import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { scoreBand } from '../format/format';

const RADIUS = 42;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

@Component({
  selector: 'app-score-ring',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="ring"
      [class]="'ring ring--' + size() + ' tone-' + (band()?.tone ?? 'none')"
      role="img"
      [attr.aria-label]="ariaLabel()"
    >
      <svg viewBox="0 0 100 100" aria-hidden="true">
        <circle class="ring__track" cx="50" cy="50" [attr.r]="radius" />
        @if (score() !== null) {
          <circle
            class="ring__fill"
            cx="50"
            cy="50"
            [attr.r]="radius"
            [attr.stroke-dasharray]="circumference"
            [attr.stroke-dashoffset]="offset()"
          />
        }
      </svg>
      <span class="ring__value" aria-hidden="true">{{ score() ?? '—' }}</span>
    </div>
    <div class="ring__caption" aria-hidden="true">
      <span class="ring__label">{{ label() }}</span>
      @if (band(); as b) {
        <span class="ring__band">{{ b.label }}</span>
      }
    </div>
  `,
  styleUrl: './score-ring.scss',
})
export class ScoreRing {
  readonly score = input<number | null>(null);
  readonly label = input.required<string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  protected readonly radius = RADIUS;
  protected readonly circumference = CIRCUMFERENCE;
  protected readonly band = computed(() => {
    const score = this.score();
    return score === null ? null : scoreBand(score);
  });
  protected readonly offset = computed(() => {
    const score = Math.max(0, Math.min(100, this.score() ?? 0));
    return CIRCUMFERENCE * (1 - score / 100);
  });
  protected readonly ariaLabel = computed(() => {
    const score = this.score();
    const band = this.band();
    return score === null || !band
      ? `${this.label()}: not analysed yet`
      : `${this.label()}: ${score} out of 100, ${band.label}`;
  });
}

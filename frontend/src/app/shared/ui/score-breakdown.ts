import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Scores } from '../../core/models/api.models';
import { formatShare } from '../format/format';
import { DIMENSION_WEIGHTS } from '../format/metrics';
import { ScoreRing } from './score-ring';

@Component({
  selector: 'app-score-breakdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScoreRing],
  template: `
    <app-score-ring [score]="scores().overall" label="Overall" size="lg" />
    <div class="dimensions">
      @for (dimension of dimensions(); track dimension.label) {
        <app-score-ring
          [score]="dimension.score"
          [label]="dimension.label"
          [size]="dimensionSize()"
        />
      }
    </div>
  `,
  styles: `
    :host,
    .dimensions {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-around;
      gap: 1.5rem;
    }
    .dimensions {
      justify-content: center;
    }
  `,
})
export class ScoreBreakdown {
  readonly scores = input.required<Scores>();
  readonly dimensionSize = input<'sm' | 'md'>('sm');
  readonly showWeights = input(true);

  protected readonly dimensions = computed(() => {
    const scores = this.scores();
    const label = (name: string, weight: number) =>
      this.showWeights() ? `${name} · ${formatShare(weight)}` : name;
    return [
      { label: label('Activity', DIMENSION_WEIGHTS.activity), score: scores.activity },
      { label: label('Structure', DIMENSION_WEIGHTS.structure), score: scores.structure },
      { label: label('Quality', DIMENSION_WEIGHTS.quality), score: scores.quality },
    ];
  });
}

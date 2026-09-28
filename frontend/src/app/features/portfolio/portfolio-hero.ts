import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PortfolioOwner, Scores } from '../../core/models/api.models';
import { ScoreRing } from '../../shared/ui/score-ring';

@Component({
  selector: 'app-portfolio-hero',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScoreRing],
  templateUrl: './portfolio-hero.html',
  styleUrl: './portfolio-hero.scss',
})
export class PortfolioHero {
  readonly owner = input.required<PortfolioOwner>();
  readonly scores = input<Scores | null>(null);

  protected readonly displayName = computed(() => this.owner().name ?? this.owner().login);
  protected readonly initials = computed(() =>
    this.displayName()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join(''),
  );
}

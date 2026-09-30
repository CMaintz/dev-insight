import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PortfolioOwner, Scores } from '../../core/models/api.models';
import { ScoreBreakdown } from '../../shared/ui/score-breakdown';

@Component({
  selector: 'app-portfolio-hero',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScoreBreakdown],
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

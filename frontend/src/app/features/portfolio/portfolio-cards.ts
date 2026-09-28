import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PortfolioProject, PortfolioRepository } from '../../core/models/api.models';
import { languageShares } from '../../shared/charts/chart-data';
import { formatShare } from '../../shared/format/format';
import { RepoFacts } from '../../shared/ui/repo-facts';
import { ScoreBadge } from '../../shared/ui/score-badge';

@Component({
  selector: 'app-portfolio-repo-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScoreBadge, RepoFacts],
  templateUrl: './portfolio-repo-card.html',
  styleUrl: './portfolio-cards.scss',
})
export class PortfolioRepoCard {
  readonly repo = input.required<PortfolioRepository>();

  protected readonly languages = computed(() =>
    languageShares(this.repo().languages)
      .slice(0, 3)
      .map((l) => `${l.language} ${formatShare(l.share)}`),
  );
}

@Component({
  selector: 'app-portfolio-project-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portfolio-project-card.html',
  styleUrl: './portfolio-cards.scss',
})
export class PortfolioProjectCard {
  readonly project = input.required<PortfolioProject>();
  readonly repoNames = input<ReadonlyMap<string, string>>(new Map());

  protected readonly imageIndex = signal(0);
  protected readonly image = computed(() => this.project().imageUrls[this.imageIndex()] ?? null);
  protected readonly linked = computed(() =>
    this.project()
      .linkedRepositoryIds.map((id) => this.repoNames().get(id))
      .filter((name): name is string => !!name),
  );

  protected show(index: number): void {
    this.imageIndex.set(index);
  }
}

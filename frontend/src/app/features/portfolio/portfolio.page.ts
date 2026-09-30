import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { PortfolioApi } from '../../core/api/portfolio.api';
import { isNotFound } from '../../core/http/problem-details';
import { Portfolio } from '../../core/models/api.models';
import { ActivityChart, LanguagesChart, ScoreEvolutionChart } from '../../shared/charts/charts';
import { formatCompact } from '../../shared/format/format';
import { EmptyState } from '../../shared/ui/empty-state';
import { LoadError } from '../../shared/ui/load-error';
import { PageSkeleton } from '../../shared/ui/page-skeleton';
import { KpiTile } from '../../shared/ui/kpi-tile';
import { safeValue } from '../../shared/util/reload-on';
import { PortfolioProjectCard, PortfolioRepoCard } from './portfolio-cards';
import { PortfolioHero } from './portfolio-hero';

export interface PortfolioHighlights {
  repositories: string;
  commits: string;
  activeWeeks: string;
  topLanguage: string;
}

export function portfolioHighlights(p: Portfolio): PortfolioHighlights {
  const commits = p.activity.reduce((sum, w) => sum + w.commits, 0);
  const top = [...p.languages].sort((a, b) => b.share - a.share)[0];
  return {
    repositories: formatCompact(p.repositories.length),
    commits: formatCompact(commits),
    activeWeeks: formatCompact(p.activity.filter((w) => w.commits > 0).length),
    topLanguage: top?.language ?? '—',
  };
}

@Component({
  selector: 'app-portfolio-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadError,
    PageSkeleton,
    RouterLink,
    ActivityChart,
    LanguagesChart,
    ScoreEvolutionChart,
    EmptyState,
    KpiTile,
    PortfolioHero,
    PortfolioProjectCard,
    PortfolioRepoCard,
  ],
  templateUrl: './portfolio.page.html',
  styleUrl: './portfolio.page.scss',
})
export class PortfolioPage {
  private readonly title = inject(Title);

  readonly handle = input.required<string>();

  protected readonly portfolio = inject(PortfolioApi).resource(this.handle);
  protected readonly data = safeValue(this.portfolio);
  protected readonly notFound = computed(() => isNotFound(this.portfolio.error()));
  protected readonly highlights = computed(() => {
    const data = this.data();
    return data ? portfolioHighlights(data) : null;
  });
  protected readonly repoNames = computed(
    () => new Map((this.data()?.repositories ?? []).map((r) => [r.id, r.name])),
  );
  protected readonly isOwnerPreview = computed(() => {
    const data = this.data();
    return !!data && !data.owner.isPublic;
  });

  constructor() {
    effect(() => {
      const owner = this.data()?.owner;
      if (owner) {
        this.title.setTitle(`${owner.name ?? owner.login} · DevInsight portfolio`);
      }
    });
  }
}

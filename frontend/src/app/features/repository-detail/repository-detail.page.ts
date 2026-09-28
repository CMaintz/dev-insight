import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RunTracker } from '../../core/analysis/run-tracker';
import { AnalysisApi } from '../../core/api/analysis.api';
import { ReposApi } from '../../core/api/repos.api';
import { isNotFound } from '../../core/http/problem-details';
import { ScopePreference } from '../../core/scope/scope-preference';
import { languageShares } from '../../shared/charts/chart-data';
import { ActivityChart, LanguagesChart, ScoreEvolutionChart } from '../../shared/charts/charts';
import { formatDate, formatInteger } from '../../shared/format/format';
import { EmptyState } from '../../shared/ui/empty-state';
import { ScopeToggle } from '../../shared/ui/scope-toggle';
import { ScoreRing } from '../../shared/ui/score-ring';
import { reloadOn, safeValue } from '../../shared/util/reload-on';
import { FeedbackList } from './feedback-list';
import { MetricBreakdown } from './metric-breakdown';

@Component({
  selector: 'app-repository-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    ActivityChart,
    LanguagesChart,
    ScoreEvolutionChart,
    EmptyState,
    ScopeToggle,
    ScoreRing,
    FeedbackList,
    MetricBreakdown,
  ],
  templateUrl: './repository-detail.page.html',
  styleUrl: './repository-detail.page.scss',
})
export class RepositoryDetailPage {
  private readonly analysisApi = inject(AnalysisApi);
  protected readonly tracker = inject(RunTracker);
  protected readonly scope = inject(ScopePreference).scope;

  /** Route parameter `:id` (bound via withComponentInputBinding). */
  readonly id = input.required<string>();

  private readonly key = computed(() => ({ repoId: this.id(), scope: this.scope() }));
  protected readonly repo = inject(ReposApi).repoResource(this.id);
  protected readonly analysis = this.analysisApi.latestResource(this.key);
  protected readonly history = this.analysisApi.historyResource(this.key);

  protected readonly repoData = safeValue(this.repo);
  protected readonly analysisData = safeValue(this.analysis);
  private readonly historyValue = safeValue(this.history);
  protected readonly historyData = computed(() => this.historyValue() ?? []);

  protected readonly notAnalysed = computed(() => isNotFound(this.analysis.error()));
  protected readonly repoMissing = computed(() => isNotFound(this.repo.error()));
  protected readonly run = computed(() => this.tracker.stateFor(this.id()));
  protected readonly languages = computed(() => languageShares(this.repoData()?.languages ?? {}));
  protected readonly dimensionScores = computed(() => {
    return { ...this.analysisData()?.scores };
  });

  protected readonly formatDate = formatDate;
  protected readonly formatInteger = formatInteger;

  constructor() {
    reloadOn(this.tracker.settledCount, this.analysis);
    reloadOn(this.tracker.settledCount, this.history);
  }

  protected async analyse(): Promise<void> {
    try {
      await this.tracker.analyse(this.id());
    } catch {
      // Surfaced by the interceptor.
    }
  }
}

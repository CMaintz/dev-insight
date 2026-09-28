import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RunTracker } from '../../core/analysis/run-tracker';
import { WorkspaceActions } from '../../core/analysis/workspace-actions';
import { DashboardApi } from '../../core/api/dashboard.api';
import { SessionStore } from '../../core/auth/session.store';
import { ScopePreference } from '../../core/scope/scope-preference';
import { vagueShare } from '../../shared/charts/chart-data';
import {
  ActivityChart,
  CommitSizeChart,
  LanguagesChart,
  ScoreEvolutionChart,
} from '../../shared/charts/charts';
import { formatShare } from '../../shared/format/format';
import { EmptyState } from '../../shared/ui/empty-state';
import { FeedbackItem } from '../../shared/ui/feedback-item';
import { KpiTile } from '../../shared/ui/kpi-tile';
import { ScopeToggle } from '../../shared/ui/scope-toggle';
import { ScoreRing } from '../../shared/ui/score-ring';
import { reloadOn, stickyValue } from '../../shared/util/reload-on';
import { FirstRunGuide, FirstRunProgress } from './first-run-guide';
import { RepoScoreTable } from './repo-score-table';

@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    ActivityChart,
    CommitSizeChart,
    LanguagesChart,
    ScoreEvolutionChart,
    EmptyState,
    FeedbackItem,
    KpiTile,
    ScopeToggle,
    ScoreRing,
    FirstRunGuide,
    RepoScoreTable,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage {
  private readonly session = inject(SessionStore);
  protected readonly scope = inject(ScopePreference).scope;
  protected readonly actions = inject(WorkspaceActions);
  protected readonly tracker = inject(RunTracker);

  protected readonly dashboard = inject(DashboardApi).resource(this.scope);
  protected readonly data = stickyValue(this.dashboard);

  protected readonly progress = computed<FirstRunProgress | null>(() => {
    const data = this.data();
    if (!data) {
      return null;
    }
    return {
      repositoryCount: data.repositoryCount,
      selectedCount: data.selectedCount,
      analyzedCount: data.analyzedCount,
      isPortfolioPublic: this.session.profile()?.isPortfolioPublic ?? false,
    };
  });
  protected readonly showGuide = computed(() => {
    const p = this.progress();
    return !!p && (p.analyzedCount === 0 || !p.isPortfolioPublic);
  });
  protected readonly vagueShare = computed(() => {
    const data = this.data();
    return data ? formatShare(vagueShare(data.commitQuality)) : '—';
  });

  constructor() {
    reloadOn(this.tracker.settledCount, this.dashboard);
    reloadOn(this.actions.importVersion, this.dashboard);
  }
}

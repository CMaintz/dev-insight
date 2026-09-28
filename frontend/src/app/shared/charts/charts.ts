import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import {
  ActivityWeek,
  CommitQuality,
  LanguageShare,
  ScorePoint,
  ScoreSnapshot,
} from '../../core/models/api.models';
import { BarChart } from './bar-chart';
import {
  activitySeries,
  activitySummary,
  activityTable,
  commitSizeBars,
  commitSizeSummary,
  commitSizeTable,
  languageBars,
  languageSummary,
  languageTable,
  scoreSeries,
  scoreSummary,
  scoreTable,
  toScoreSamples,
} from './chart-data';
import { ChartFrame } from './chart-frame';
import { TimeSeriesChart } from './time-series-chart';

@Component({
  selector: 'app-activity-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartFrame, TimeSeriesChart],
  template: `
    <app-chart-frame
      [heading]="heading()"
      [summary]="summary()"
      [table]="table()"
      [empty]="weeks().length === 0"
      emptyText="No commits in the analysed period yet."
    >
      <app-time-series-chart [series]="series()" />
    </app-chart-frame>
  `,
})
export class ActivityChart {
  readonly weeks = input.required<ActivityWeek[]>();
  readonly heading = input('Commits per week');

  protected readonly series = computed(() => activitySeries(this.weeks()));
  protected readonly summary = computed(() => activitySummary(this.weeks()));
  protected readonly table = computed(() => activityTable(this.weeks()));
}

@Component({
  selector: 'app-score-evolution-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartFrame, TimeSeriesChart],
  template: `
    <app-chart-frame
      [heading]="heading()"
      [summary]="summary()"
      [table]="table()"
      [empty]="samples().length < 2"
      [emptyText]="summary()"
      [height]="300"
    >
      <app-time-series-chart [series]="series()" [yMax]="100" />
    </app-chart-frame>
  `,
})
export class ScoreEvolutionChart {
  readonly points = input.required<(ScorePoint | ScoreSnapshot)[]>();
  readonly heading = input('Score evolution');

  protected readonly samples = computed(() => toScoreSamples(this.points()));
  protected readonly series = computed(() => scoreSeries(this.samples()));
  protected readonly summary = computed(() => scoreSummary(this.samples()));
  protected readonly table = computed(() => scoreTable(this.samples()));
}

@Component({
  selector: 'app-languages-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartFrame, BarChart],
  template: `
    <app-chart-frame
      [heading]="heading()"
      [summary]="summary()"
      [table]="table()"
      [empty]="languages().length === 0"
      emptyText="No language data yet — import and select repositories."
      [height]="height()"
    >
      <app-bar-chart [data]="bars()" orientation="horizontal" unit="%" [max]="100" />
    </app-chart-frame>
  `,
})
export class LanguagesChart {
  readonly languages = input.required<LanguageShare[]>();
  readonly heading = input('Languages');

  protected readonly bars = computed(() => languageBars(this.languages()));
  protected readonly height = computed(() => Math.max(120, this.bars().length * 36 + 16));
  protected readonly summary = computed(() => languageSummary(this.languages()));
  protected readonly table = computed(() => languageTable(this.languages()));
}

@Component({
  selector: 'app-commit-size-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartFrame, BarChart],
  template: `
    <app-chart-frame
      heading="Commit size distribution"
      [summary]="summary()"
      [table]="table()"
      [empty]="quality().commits === 0"
      emptyText="No commits analysed yet."
    >
      <app-bar-chart [data]="bars()" />
    </app-chart-frame>
  `,
})
export class CommitSizeChart {
  readonly quality = input.required<CommitQuality>();

  protected readonly bars = computed(() => commitSizeBars(this.quality()));
  protected readonly summary = computed(() => commitSizeSummary(this.quality()));
  protected readonly table = computed(() => commitSizeTable(this.quality()));
}

import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Color, LegendPosition, LineChartModule, ScaleType } from '@swimlane/ngx-charts';
import { ThemeService } from '../../core/theme/theme.service';
import { ChartSeries } from './chart-data';
import { prefersReducedMotion, seriesColors } from './chart-palette';

/** Line chart over a time axis (2px lines, hover crosshair + tooltip from ngx-charts). */
@Component({
  selector: 'app-time-series-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LineChartModule],
  template: `
    <ngx-charts-line-chart
      [results]="series()"
      [scheme]="scheme()"
      [legend]="series().length > 1"
      [legendTitle]="''"
      [legendPosition]="legendPosition"
      [xAxis]="true"
      [yAxis]="true"
      [showGridLines]="true"
      [roundDomains]="true"
      [autoScale]="false"
      [yScaleMin]="0"
      [yScaleMax]="yMax() ?? 0"
      [animations]="animations"
      [xAxisTickFormatting]="formatTick"
    />
  `,
  styles: ':host { display: block; width: 100%; height: 100%; }',
})
export class TimeSeriesChart {
  private readonly theme = inject(ThemeService);

  readonly series = input.required<ChartSeries[]>();
  /** Fixed upper bound (e.g. 100 for scores); undefined lets the data decide. */
  readonly yMax = input<number | undefined>(undefined);

  protected readonly legendPosition = LegendPosition.Below;
  protected readonly animations = !prefersReducedMotion();
  protected readonly scheme = computed<Color>(() => ({
    name: 'devinsight',
    selectable: false,
    group: ScaleType.Ordinal,
    domain: seriesColors(this.theme.effective(), this.series().length),
  }));

  protected readonly formatTick = (value: Date | string): string =>
    value instanceof Date
      ? value.toLocaleDateString('en', { month: 'short', year: '2-digit', timeZone: 'UTC' })
      : String(value);
}

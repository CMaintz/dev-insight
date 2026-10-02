import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Color, LineChartModule, ScaleType } from '@swimlane/ngx-charts';
import { ThemeService } from '../../core/theme/theme.service';
import { ChartSeries } from './chart-data';
import { prefersReducedMotion, seriesColors } from './chart-palette';

@Component({
  selector: 'app-time-series-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LineChartModule],
  template: `
    <div class="plot">
      <ngx-charts-line-chart
        [results]="series()"
        [scheme]="scheme()"
        [legend]="false"
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
    </div>
    @if (legend().length > 1) {
      <ul class="legend">
        @for (item of legend(); track item.name) {
          <li class="legend__item">
            <span class="legend__swatch" [style.background]="item.color"></span>{{ item.name }}
          </li>
        }
      </ul>
    }
  `,
  styleUrl: './time-series-chart.scss',
})
export class TimeSeriesChart {
  private readonly theme = inject(ThemeService);

  readonly series = input.required<ChartSeries[]>();
  readonly yMax = input<number | undefined>(undefined);

  protected readonly animations = !prefersReducedMotion();
  private readonly colors = computed(() =>
    seriesColors(this.theme.effective(), this.series().length),
  );
  protected readonly scheme = computed<Color>(() => ({
    name: 'devinsight',
    selectable: false,
    group: ScaleType.Ordinal,
    domain: this.colors(),
  }));
  protected readonly legend = computed(() =>
    this.series().map((s, i) => ({ name: s.name, color: this.colors()[i] })),
  );

  protected readonly formatTick = (value: Date | string): string =>
    value instanceof Date
      ? value.toLocaleDateString('en', { month: 'short', year: '2-digit', timeZone: 'UTC' })
      : String(value);
}

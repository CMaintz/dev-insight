import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { BarChartModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { ThemeService } from '../../core/theme/theme.service';
import { ChartPoint } from './chart-data';
import { prefersReducedMotion, seriesColors } from './chart-palette';

/** Single-series bar chart (one colour, slot 1) with values labelled at the bar tips. */
@Component({
  selector: 'app-bar-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarChartModule],
  template: `
    @if (orientation() === 'horizontal') {
      <ngx-charts-bar-horizontal
        [results]="data()"
        [scheme]="scheme()"
        [xAxis]="false"
        [yAxis]="true"
        [showGridLines]="false"
        [showDataLabel]="true"
        [dataLabelFormatting]="formatValue()"
        [barPadding]="10"
        [roundEdges]="true"
        [xScaleMax]="max() ?? 0"
        [animations]="animations"
      />
    } @else {
      <ngx-charts-bar-vertical
        [results]="data()"
        [scheme]="scheme()"
        [xAxis]="true"
        [yAxis]="true"
        [showGridLines]="true"
        [showDataLabel]="true"
        [barPadding]="24"
        [roundEdges]="true"
        [animations]="animations"
      />
    }
  `,
  styles: ':host { display: block; width: 100%; height: 100%; }',
})
export class BarChart {
  private readonly theme = inject(ThemeService);

  readonly data = input.required<ChartPoint[]>();
  readonly orientation = input<'horizontal' | 'vertical'>('vertical');
  readonly unit = input('');
  readonly max = input<number | undefined>(undefined);

  protected readonly animations = !prefersReducedMotion();
  protected readonly scheme = computed<Color>(() => ({
    name: 'devinsight-single',
    selectable: false,
    group: ScaleType.Ordinal,
    domain: seriesColors(this.theme.effective(), 1),
  }));
  protected readonly formatValue = computed(() => {
    const unit = this.unit();
    return (value: number) => `${value}${unit}`;
  });
}

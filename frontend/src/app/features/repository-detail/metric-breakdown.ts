import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Metric } from '../../core/models/api.models';
import { formatShare } from '../../shared/format/format';
import { formatMetricValue, groupMetrics, metricLabel } from '../../shared/format/metrics';

interface MetricRow {
  name: string;
  label: string;
  value: string;
  points: number | null;
  weight: string;
  scored: boolean;
}

function toRow(metric: Metric): MetricRow {
  return {
    name: metric.name,
    label: metricLabel(metric.name),
    value: formatMetricValue(metric),
    points: metric.includedInScore ? metric.points : null,
    weight: metric.includedInScore && metric.weight !== null ? formatShare(metric.weight) : '—',
    scored: metric.includedInScore,
  };
}

@Component({
  selector: 'app-metric-breakdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './metric-breakdown.html',
  styleUrl: './metric-breakdown.scss',
})
export class MetricBreakdown {
  readonly metrics = input.required<Metric[]>();
  readonly dimensionScores = input<Partial<Record<string, number>>>({});

  protected readonly groups = computed(() =>
    groupMetrics(this.metrics()).map((group) => ({
      ...group,
      score: this.dimensionScores()[group.category] ?? null,
      rows: group.metrics.map(toRow),
    })),
  );
}

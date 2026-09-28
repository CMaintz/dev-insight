import { Metric, MetricCategory } from '../../core/models/api.models';
import { formatDecimal, formatInteger, formatShare } from './format';

export const DIMENSION_WEIGHTS = { activity: 0.3, structure: 0.3, quality: 0.4 } as const;

export const CATEGORY_LABELS: Record<MetricCategory, string> = {
  activity: 'Activity',
  commitQuality: 'Commit quality',
  structure: 'Structure',
  quality: 'Quality',
};

const CATEGORY_ORDER: readonly MetricCategory[] = [
  'activity',
  'commitQuality',
  'structure',
  'quality',
];

const METRIC_LABELS: Record<string, string> = {
  total_commits: 'Total commits',
  days_since_last_commit: 'Days since last commit',
  commits_per_week_12w: 'Commits per week (last 12 weeks)',
  commits_per_week_lifetime: 'Commits per week (lifetime)',
  active_weeks_ratio_26w: 'Active weeks (last 26)',
  is_dormant: 'Dormant',
  commit_message_quality: 'Commit message quality',
  vague_commit_ratio: 'Vague commit messages',
  vague_commit_count: 'Vague commits',
  average_commit_size: 'Average commit size (lines)',
  commit_size_stddev: 'Commit size spread (std. dev.)',
  large_commit_ratio: 'Large commits (≥250 lines)',
  commits_size_xs: 'Commits < 10 lines',
  commits_size_s: 'Commits 10–49 lines',
  commits_size_m: 'Commits 50–249 lines',
  commits_size_l: 'Commits 250–999 lines',
  commits_size_xl: 'Commits ≥ 1000 lines',
  source_file_count: 'Source files',
  total_loc: 'Lines of code',
  median_file_loc: 'Median file length (lines)',
  largest_file_loc: 'Largest file (lines)',
  files_over_500_loc: 'Files over 500 lines',
  large_file_ratio: 'Share of files over 500 lines',
  max_folder_depth: 'Deepest folder nesting',
  average_folder_depth: 'Average folder depth',
  monolith_indicator: 'Monolith indicator',
  has_tests: 'Has tests',
  test_file_ratio: 'Test files share',
  has_readme: 'Has README',
  has_lint_config: 'Has lint/format config',
  has_ci: 'Has CI pipeline',
  has_license: 'Has license',
  contributor_count: 'Contributors',
};

function humanise(key: string): string {
  const words = key.replace(/_/g, ' ').trim();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

export function metricLabel(name: string): string {
  return METRIC_LABELS[name] ?? humanise(name);
}

function isBooleanMetric(name: string): boolean {
  return name.startsWith('has_') || name.startsWith('is_');
}

function isRatioMetric(name: string): boolean {
  return name.endsWith('_ratio') || name.includes('_ratio_') || name === 'monolith_indicator';
}

export function formatMetricValue(metric: Pick<Metric, 'name' | 'value'>): string {
  if (isBooleanMetric(metric.name)) {
    return metric.value >= 1 ? 'Yes' : 'No';
  }
  if (isRatioMetric(metric.name) && metric.value >= 0 && metric.value <= 1) {
    return formatShare(metric.value);
  }
  return Number.isInteger(metric.value) ? formatInteger(metric.value) : formatDecimal(metric.value);
}

export interface MetricGroup {
  category: MetricCategory;
  label: string;
  metrics: Metric[];
}

export function groupMetrics(metrics: readonly Metric[]): MetricGroup[] {
  return CATEGORY_ORDER.map((category) => ({
    category,
    label: CATEGORY_LABELS[category],
    metrics: metrics
      .filter((metric) => metric.category === category)
      .sort(
        (a, b) =>
          Number(b.includedInScore) - Number(a.includedInScore) ||
          (b.weight ?? 0) - (a.weight ?? 0),
      ),
  })).filter((group) => group.metrics.length > 0);
}

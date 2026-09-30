import {
  ActivityWeek,
  CommitQuality,
  LanguageShare,
  ScorePoint,
  ScoreSnapshot,
} from '../../core/models/api.models';
import { formatDate, formatInteger, formatShare, parseApiDate } from '../format/format';

export interface ChartPoint {
  name: string | Date;
  value: number;
}

export interface ChartSeries {
  name: string;
  series: ChartPoint[];
}

export interface TableData {
  columns: string[];
  rows: (string | number)[][];
}

export function activitySeries(weeks: readonly ActivityWeek[]): ChartSeries[] {
  return [
    {
      name: 'Commits',
      series: weeks.map((week) => ({ name: parseApiDate(week.weekStart), value: week.commits })),
    },
  ];
}

export function activitySummary(weeks: readonly ActivityWeek[]): string {
  if (weeks.length === 0) {
    return 'No commit activity recorded yet.';
  }
  const total = weeks.reduce((sum, week) => sum + week.commits, 0);
  const busiest = weeks.reduce((best, week) => (week.commits > best.commits ? week : best));
  const active = weeks.filter((week) => week.commits > 0).length;
  return (
    `${formatInteger(total)} commits over ${weeks.length} weeks (${active} active). ` +
    `Busiest week: ${formatDate(busiest.weekStart)} with ${formatInteger(busiest.commits)} commits.`
  );
}

export function activityTable(weeks: readonly ActivityWeek[]): TableData {
  return {
    columns: ['Week of', 'Commits', 'Lines added', 'Lines deleted'],
    rows: weeks.map((w) => [formatDate(w.weekStart), w.commits, w.additions, w.deletions]),
  };
}

type ScoreSample = Omit<ScorePoint, 'date'> & { date: string };

export function toScoreSamples(points: readonly (ScorePoint | ScoreSnapshot)[]): ScoreSample[] {
  return points.map((p) => ({
    date: 'date' in p ? p.date : p.createdAt,
    overall: p.overall,
    activity: p.activity,
    structure: p.structure,
    quality: p.quality,
  }));
}

const SCORE_SERIES_IN_COLOUR_SLOT_ORDER = [
  { key: 'overall', name: 'Overall' },
  { key: 'activity', name: 'Activity' },
  { key: 'structure', name: 'Structure' },
  { key: 'quality', name: 'Quality' },
] as const;

export function scoreSeries(samples: readonly ScoreSample[]): ChartSeries[] {
  return SCORE_SERIES_IN_COLOUR_SLOT_ORDER.map(({ key, name }) => ({
    name,
    series: samples.map((s) => ({ name: parseApiDate(s.date), value: s[key] })),
  }));
}

export function scoreSummary(samples: readonly ScoreSample[]): string {
  if (samples.length === 0) {
    return 'No score history yet — run an analysis to start tracking.';
  }
  const first = samples[0];
  const last = samples[samples.length - 1];
  if (samples.length === 1) {
    return `One snapshot so far: overall score ${first.overall} on ${formatDate(first.date)}.`;
  }
  const delta = last.overall - first.overall;
  const trend =
    delta === 0 ? 'held steady at' : delta > 0 ? `rose by ${delta} to` : `fell by ${-delta} to`;
  return (
    `Overall score ${trend} ${last.overall} between ${formatDate(first.date)} ` +
    `and ${formatDate(last.date)} (${samples.length} snapshots).`
  );
}

export function scoreTable(samples: readonly ScoreSample[]): TableData {
  return {
    columns: ['Date', 'Overall', 'Activity', 'Structure', 'Quality'],
    rows: samples.map((s) => [formatDate(s.date), s.overall, s.activity, s.structure, s.quality]),
  };
}

export function languageShares(bytes: Readonly<Record<string, number>>): LanguageShare[] {
  const total = Object.values(bytes).reduce((sum, b) => sum + b, 0);
  if (total <= 0) {
    return [];
  }
  return Object.entries(bytes)
    .map(([language, b]) => ({ language, bytes: b, share: b / total }))
    .sort((a, b) => b.bytes - a.bytes);
}

export function languageBars(languages: readonly LanguageShare[], max = 6): ChartPoint[] {
  const sorted = [...languages].sort((a, b) => b.share - a.share);
  const head = sorted.slice(0, max).map((l) => ({ name: l.language, value: pct(l.share) }));
  const tail = sorted.slice(max).reduce((sum, l) => sum + l.share, 0);
  return tail > 0 ? [...head, { name: 'Other', value: pct(tail) }] : head;
}

function pct(share: number): number {
  return Math.round(share * 1000) / 10;
}

export function languageSummary(languages: readonly LanguageShare[]): string {
  if (languages.length === 0) {
    return 'No language data yet.';
  }
  const top = [...languages]
    .sort((a, b) => b.share - a.share)
    .slice(0, 3)
    .map((l) => `${l.language} ${formatShare(l.share)}`);
  return `Most code is written in ${top.join(', ')}.`;
}

export function languageTable(languages: readonly LanguageShare[]): TableData {
  return {
    columns: ['Language', 'Share', 'Bytes'],
    rows: [...languages]
      .sort((a, b) => b.share - a.share)
      .map((l) => [l.language, formatShare(l.share), l.bytes]),
  };
}

const SIZE_BUCKETS = [
  { key: 'xs', label: 'XS (<10)' },
  { key: 's', label: 'S (10–49)' },
  { key: 'm', label: 'M (50–249)' },
  { key: 'l', label: 'L (250–999)' },
  { key: 'xl', label: 'XL (≥1000)' },
] as const;

export function commitSizeBars(quality: CommitQuality): ChartPoint[] {
  return SIZE_BUCKETS.map(({ key, label }) => ({ name: label, value: quality.sizes[key] }));
}

export function vagueShare(quality: CommitQuality): number {
  return quality.commits > 0 ? quality.vagueCommits / quality.commits : 0;
}

export function commitSizeSummary(quality: CommitQuality): string {
  if (quality.commits === 0) {
    return 'No commits analysed yet.';
  }
  const bars = commitSizeBars(quality);
  const mode = bars.reduce((best, bar) => (bar.value > best.value ? bar : best));
  return (
    `${formatInteger(quality.commits)} commits; most are ${mode.name} lines changed. ` +
    `${formatShare(vagueShare(quality))} have vague messages.`
  );
}

export function commitSizeTable(quality: CommitQuality): TableData {
  return {
    columns: ['Size (lines changed)', 'Commits'],
    rows: commitSizeBars(quality).map((bar) => [String(bar.name), bar.value]),
  };
}

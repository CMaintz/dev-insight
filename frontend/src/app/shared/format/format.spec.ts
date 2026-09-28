import {
  formatCompact,
  formatDate,
  formatDecimal,
  formatRelative,
  formatShare,
  formatShortDate,
  scoreBand,
} from './format';
import { formatMetricValue, groupMetrics, metricLabel } from './metrics';

describe('format helpers', () => {
  it('formats numbers', () => {
    expect(formatCompact(1284)).toBe('1,284');
    expect(formatCompact(12_900)).toBe('12.9K');
    expect(formatDecimal(2.345)).toBe('2.3');
    expect(formatShare(0.4213)).toBe('42%');
  });

  it('formats dates in UTC', () => {
    expect(formatDate('2026-09-27')).toBe('Sep 27, 2026');
    expect(formatDate('2026-09-27T23:30:00+00:00')).toBe('Sep 27, 2026');
    expect(formatDate(null)).toBe('—');
    expect(formatDate('garbage')).toBe('—');
    expect(formatShortDate('2026-01-05')).toBe('Jan 5');
    expect(formatShortDate('nope')).toBe('nope');
  });

  it('formats relative time', () => {
    const now = Date.parse('2026-09-27T12:00:00Z');
    expect(formatRelative(null, now)).toBe('never');
    expect(formatRelative('bad', now)).toBe('never');
    expect(formatRelative('2026-09-27T08:00:00Z', now)).toBe('today');
    expect(formatRelative('2026-09-26T08:00:00Z', now)).toBe('yesterday');
    expect(formatRelative('2026-09-20T12:00:00Z', now)).toBe('7 days ago');
    expect(formatRelative('2026-08-20T12:00:00Z', now)).toBe('1 month ago');
    expect(formatRelative('2026-03-01T12:00:00Z', now)).toBe('7 months ago');
    expect(formatRelative('2025-06-01T12:00:00Z', now)).toBe('1 year ago');
    expect(formatRelative('2022-06-01T12:00:00Z', now)).toBe('4 years ago');
  });

  it('bands scores', () => {
    expect(scoreBand(75)).toEqual({ tone: 'good', label: 'Strong' });
    expect(scoreBand(50).tone).toBe('fair');
    expect(scoreBand(49).tone).toBe('weak');
  });
});

describe('metric helpers', () => {
  it('labels known and unknown metric keys', () => {
    expect(metricLabel('vague_commit_ratio')).toBe('Vague commit messages');
    expect(metricLabel('brand_new_metric')).toBe('Brand new metric');
  });

  it('formats values by what the key measures', () => {
    expect(formatMetricValue({ name: 'has_tests', value: 1 })).toBe('Yes');
    expect(formatMetricValue({ name: 'is_dormant', value: 0 })).toBe('No');
    expect(formatMetricValue({ name: 'vague_commit_ratio', value: 0.25 })).toBe('25%');
    expect(formatMetricValue({ name: 'active_weeks_ratio_26w', value: 0.5 })).toBe('50%');
    expect(formatMetricValue({ name: 'total_commits', value: 1200 })).toBe('1,200');
    expect(formatMetricValue({ name: 'average_commit_size', value: 12.34 })).toBe('12.3');
  });

  it('groups by category in fixed order, scored and heavier metrics first', () => {
    const groups = groupMetrics([
      {
        name: 'b',
        category: 'quality',
        value: 1,
        includedInScore: false,
        points: null,
        weight: null,
      },
      { name: 'a', category: 'activity', value: 1, includedInScore: true, points: 50, weight: 0.2 },
      { name: 'c', category: 'quality', value: 1, includedInScore: true, points: 50, weight: 0.3 },
      { name: 'd', category: 'quality', value: 1, includedInScore: true, points: 50, weight: 0.6 },
    ]);
    expect(groups.map((g) => g.category)).toEqual(['activity', 'quality']);
    expect(groups[1].metrics.map((m) => m.name)).toEqual(['d', 'c', 'b']);
  });
});

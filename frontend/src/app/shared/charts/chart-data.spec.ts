import { ActivityWeek, CommitQuality } from '../../core/models/api.models';
import {
  activitySeries,
  activitySummary,
  activityTable,
  commitSizeBars,
  commitSizeSummary,
  commitSizeTable,
  languageBars,
  languageShares,
  languageSummary,
  languageTable,
  scoreSeries,
  scoreSummary,
  scoreTable,
  toScoreSamples,
  vagueShare,
} from './chart-data';
import { CATEGORICAL, seriesColors } from './chart-palette';

const weeks: ActivityWeek[] = [
  { weekStart: '2026-09-07', commits: 2, additions: 10, deletions: 1 },
  { weekStart: '2026-09-14', commits: 0, additions: 0, deletions: 0 },
  { weekStart: '2026-09-21', commits: 5, additions: 50, deletions: 9 },
];

describe('activity mapping', () => {
  it('builds one dated series', () => {
    const [series] = activitySeries(weeks);
    expect(series.name).toBe('Commits');
    expect(series.series[2]).toEqual({ name: new Date('2026-09-21T00:00:00Z'), value: 5 });
  });

  it('summarises totals and the busiest week', () => {
    expect(activitySummary(weeks)).toBe(
      '7 commits over 3 weeks (2 active). Busiest week: Sep 21, 2026 with 5 commits.',
    );
    expect(activitySummary([])).toBe('No commit activity recorded yet.');
  });

  it('provides a table twin', () => {
    expect(activityTable(weeks).rows[0]).toEqual(['Sep 7, 2026', 2, 10, 1]);
  });
});

describe('score mapping', () => {
  const samples = toScoreSamples([
    { date: '2026-01-01', overall: 50, activity: 40, structure: 60, quality: 55 },
    {
      analysisId: 'a',
      createdAt: '2026-06-01T00:00:00Z',
      overall: 62,
      activity: 50,
      structure: 60,
      quality: 70,
    },
  ]);

  it('normalises points and snapshots', () => {
    expect(samples.map((s) => s.date)).toEqual(['2026-01-01', '2026-06-01T00:00:00Z']);
  });

  it('keeps series in fixed entity order', () => {
    expect(scoreSeries(samples).map((s) => s.name)).toEqual([
      'Overall',
      'Activity',
      'Structure',
      'Quality',
    ]);
  });

  it('describes the trend', () => {
    expect(scoreSummary(samples)).toContain('rose by 12 to 62');
    expect(scoreSummary([...samples].reverse())).toContain('fell by 12 to 50');
    expect(scoreSummary([samples[0], { ...samples[1], overall: 50 }])).toContain(
      'held steady at 50',
    );
    expect(scoreSummary(samples.slice(0, 1))).toContain('One snapshot so far');
    expect(scoreSummary([])).toContain('No score history');
    expect(scoreTable(samples).rows).toHaveLength(2);
  });
});

describe('language mapping', () => {
  it('turns bytes into sorted shares', () => {
    const shares = languageShares({ CSS: 100, TypeScript: 300 });
    expect(shares.map((s) => [s.language, s.share])).toEqual([
      ['TypeScript', 0.75],
      ['CSS', 0.25],
    ]);
    expect(languageShares({})).toEqual([]);
  });

  it('folds the tail into Other', () => {
    const langs = ['a', 'b', 'c', 'd'].map((language, i) => ({
      language,
      bytes: 1,
      share: [0.5, 0.3, 0.15, 0.05][i],
    }));
    expect(languageBars(langs, 2)).toEqual([
      { name: 'a', value: 50 },
      { name: 'b', value: 30 },
      { name: 'Other', value: 20 },
    ]);
    expect(languageBars(langs.slice(0, 1))).toEqual([{ name: 'a', value: 50 }]);
  });

  it('summarises the top languages', () => {
    expect(languageSummary([{ language: 'C#', bytes: 1, share: 0.6 }])).toBe(
      'Most code is written in C# 60%.',
    );
    expect(languageSummary([])).toBe('No language data yet.');
    expect(languageTable([{ language: 'C#', bytes: 10, share: 1 }]).rows).toEqual([
      ['C#', '100%', 10],
    ]);
  });
});

describe('commit size mapping', () => {
  const quality: CommitQuality = {
    commits: 20,
    vagueCommits: 5,
    sizes: { xs: 2, s: 10, m: 6, l: 2, xl: 0 },
  };

  it('maps buckets in order', () => {
    expect(commitSizeBars(quality).map((b) => b.value)).toEqual([2, 10, 6, 2, 0]);
    expect(commitSizeTable(quality).rows[1]).toEqual(['S (10–49)', 10]);
  });

  it('computes the vague share and summary', () => {
    expect(vagueShare(quality)).toBe(0.25);
    expect(vagueShare({ ...quality, commits: 0 })).toBe(0);
    expect(commitSizeSummary(quality)).toBe(
      '20 commits; most are S (10–49) lines changed. 25% have vague messages.',
    );
    expect(commitSizeSummary({ ...quality, commits: 0 })).toBe('No commits analysed yet.');
  });
});

describe('seriesColors', () => {
  it('takes slots in fixed order and clamps the count', () => {
    expect(seriesColors('light', 2)).toEqual(CATEGORICAL.light.slice(0, 2));
    expect(seriesColors('dark', 0)).toEqual([CATEGORICAL.dark[0]]);
    expect(seriesColors('light', 20)).toHaveLength(8);
  });
});

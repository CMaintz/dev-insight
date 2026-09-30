import { aRepo } from '../../../testing/fixtures';
import { EMPTY_FILTER, distinctLanguages, filterRepositories, sortByActivity } from './repo-filter';

const repos = [
  aRepo({ id: '1', name: 'alpha', language: 'TypeScript', isSelected: true }),
  aRepo({
    id: '2',
    name: 'beta',
    description: 'Kotlin app',
    language: 'Kotlin',
    isSelected: false,
  }),
  aRepo({ id: '3', name: 'gamma', description: null, language: null, lastActivity: null }),
];

describe('filterRepositories', () => {
  it('returns everything for the empty filter', () => {
    expect(filterRepositories(repos, EMPTY_FILTER)).toHaveLength(3);
  });

  it('searches name and description case-insensitively', () => {
    expect(
      filterRepositories(repos, { ...EMPTY_FILTER, query: 'KOTLIN' }).map((r) => r.id),
    ).toEqual(['2']);
    expect(filterRepositories(repos, { ...EMPTY_FILTER, query: 'gam' }).map((r) => r.id)).toEqual([
      '3',
    ]);
  });

  it('filters by language and selection', () => {
    expect(filterRepositories(repos, { ...EMPTY_FILTER, language: 'Kotlin' })).toHaveLength(1);
    expect(
      filterRepositories(repos, { ...EMPTY_FILTER, selectedOnly: true }).map((r) => r.id),
    ).toEqual(['1', '3']);
  });
});

describe('sortByActivity', () => {
  it('puts recent first and never-active last', () => {
    const sorted = sortByActivity([
      aRepo({ id: 'old', lastActivity: '2024-01-01T00:00:00Z' }),
      aRepo({ id: 'none', lastActivity: null }),
      aRepo({ id: 'new', lastActivity: '2026-01-01T00:00:00Z' }),
    ]);
    expect(sorted.map((r) => r.id)).toEqual(['new', 'old', 'none']);
  });
});

describe('distinctLanguages', () => {
  it('lists unique non-null languages alphabetically', () => {
    expect(distinctLanguages(repos)).toEqual(['Kotlin', 'TypeScript']);
  });
});

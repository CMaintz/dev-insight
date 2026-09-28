import { Repository } from '../../core/models/api.models';

export interface RepoFilter {
  query: string;
  language: string;
  selectedOnly: boolean;
}

export const EMPTY_FILTER: RepoFilter = { query: '', language: '', selectedOnly: false };

/** Case-insensitive search over name/description, plus language and selection filters. */
export function filterRepositories(repos: readonly Repository[], filter: RepoFilter): Repository[] {
  const query = filter.query.trim().toLowerCase();
  return repos.filter((repo) => {
    if (filter.selectedOnly && !repo.isSelected) {
      return false;
    }
    if (filter.language && repo.language !== filter.language) {
      return false;
    }
    if (!query) {
      return true;
    }
    return (
      repo.name.toLowerCase().includes(query) ||
      (repo.description ?? '').toLowerCase().includes(query)
    );
  });
}

/** Most recently active first; never-active repositories last, then by name. */
export function sortByActivity(repos: readonly Repository[]): Repository[] {
  return [...repos].sort((a, b) => {
    const at = a.lastActivity ? Date.parse(a.lastActivity) : -Infinity;
    const bt = b.lastActivity ? Date.parse(b.lastActivity) : -Infinity;
    return bt - at || a.name.localeCompare(b.name);
  });
}

export function distinctLanguages(repos: readonly Repository[]): string[] {
  return [...new Set(repos.map((r) => r.language).filter((l): l is string => !!l))].sort();
}

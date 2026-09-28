import { Injectable, signal } from '@angular/core';
import { Scope, ScopeParam } from '../models/api.models';

/** Maps a response-body scope to the query-parameter form. */
export function toScopeParam(scope: Scope): ScopeParam {
  return scope === 'userContribution' ? 'user' : 'repo';
}

export const SCOPE_LABELS: Record<ScopeParam, string> = {
  repo: 'Whole repository',
  user: 'My contributions',
};

/** App-wide scope choice so the dashboard and repository pages stay in sync. */
@Injectable({ providedIn: 'root' })
export class ScopePreference {
  readonly scope = signal<ScopeParam>('repo');
}

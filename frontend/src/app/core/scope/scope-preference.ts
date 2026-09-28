import { Injectable, signal } from '@angular/core';
import { ScopeParam } from '../models/api.models';

export const SCOPE_LABELS: Record<ScopeParam, string> = {
  repo: 'Whole repository',
  user: 'My contributions',
};

@Injectable({ providedIn: 'root' })
export class ScopePreference {
  readonly scope = signal<ScopeParam>('repo');
}

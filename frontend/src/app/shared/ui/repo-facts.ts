import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Repository } from '../../core/models/api.models';
import { formatCompact, formatRelative } from '../format/format';

type RepoFactsSource = Pick<Repository, 'stars' | 'forks' | 'lastActivity'>;

@Component({
  selector: 'app-repo-facts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="facts list-reset" aria-label="Repository facts">
      <ng-content />
      <li>
        <span aria-hidden="true">★</span> {{ stars() }}<span class="visually-hidden"> stars</span>
      </li>
      <li>
        <span aria-hidden="true">⑂</span> {{ forks() }}<span class="visually-hidden"> forks</span>
      </li>
      <li>Active {{ lastActivity() }}</li>
    </ul>
  `,
  styles: `
    :host {
      display: block;
    }
    .facts {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.375rem 1rem;
      color: var(--text-secondary);
      font-size: 0.8125rem;
    }
  `,
})
export class RepoFacts {
  readonly repo = input.required<RepoFactsSource>();

  protected readonly stars = computed(() => formatCompact(this.repo().stars));
  protected readonly forks = computed(() => formatCompact(this.repo().forks));
  protected readonly lastActivity = computed(() => formatRelative(this.repo().lastActivity));
}

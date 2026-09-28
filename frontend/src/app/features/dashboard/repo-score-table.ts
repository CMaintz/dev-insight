import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RepositorySummary } from '../../core/models/api.models';
import { formatRelative } from '../../shared/format/format';
import { ScoreBadge } from '../../shared/ui/score-badge';

@Component({
  selector: 'app-repo-score-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ScoreBadge],
  template: `
    <div class="table-wrap">
      <table class="data-table">
        <caption class="visually-hidden">
          Repositories and their scores
        </caption>
        <thead>
          <tr>
            <th scope="col">Repository</th>
            <th scope="col">Overall</th>
            <th scope="col">Activity</th>
            <th scope="col">Structure</th>
            <th scope="col">Quality</th>
            <th scope="col">Analysed</th>
          </tr>
        </thead>
        <tbody>
          @for (row of rows(); track row.repository.id) {
            <tr [class.is-unselected]="!row.repository.isSelected">
              <th scope="row">
                <span class="repo-name">
                  <a [routerLink]="['/repositories', row.repository.id]">{{
                    row.repository.name
                  }}</a>
                  @if (!row.repository.isSelected) {
                    <span class="chip">not selected</span>
                  }
                </span>
              </th>
              <td><app-score-badge [score]="row.scores?.overall ?? null" label="Overall" /></td>
              <td><app-score-badge [score]="row.scores?.activity ?? null" label="Activity" /></td>
              <td><app-score-badge [score]="row.scores?.structure ?? null" label="Structure" /></td>
              <td><app-score-badge [score]="row.scores?.quality ?? null" label="Quality" /></td>
              <td class="muted">{{ relative(row.analyzedAt) }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    th[scope='row'] {
      background: none;
      color: var(--text-primary);
      font-weight: 550;
    }
    .repo-name {
      display: inline-flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
    }
    .is-unselected {
      opacity: 0.7;
    }
  `,
})
export class RepoScoreTable {
  readonly rows = input.required<RepositorySummary[]>();

  protected readonly relative = (value: string | null) =>
    value ? formatRelative(value) : 'not yet';
}

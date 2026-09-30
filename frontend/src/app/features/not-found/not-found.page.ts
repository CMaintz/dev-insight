import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../../shared/ui/empty-state';

@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, RouterLink],
  template: `
    <div class="page">
      <h1 class="visually-hidden">Page not found</h1>
      <app-empty-state
        icon="🧭"
        heading="This page does not exist"
        text="The link may be outdated or mistyped."
      >
        <a class="btn btn--primary" routerLink="/">Back to the start</a>
      </app-empty-state>
    </div>
  `,
})
export class NotFoundPage {}

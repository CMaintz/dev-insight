import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SessionStore } from '../../core/auth/session.store';
import { jumpToFragment } from '../../shared/util/jump-to';

@Component({
  selector: 'app-landing-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './landing.page.html',
  styleUrl: './landing.page.scss',
})
export class LandingPage {
  protected readonly session = inject(SessionStore);

  protected readonly steps = [
    { title: 'Import', text: 'Connect GitHub and pull in your repositories in one click.' },
    {
      title: 'Select',
      text: 'Choose which projects represent you. Forks and experiments stay out.',
    },
    {
      title: 'Analyse',
      text: 'Activity, structure and quality metrics — for the whole repo or just your commits.',
    },
    { title: 'Understand', text: 'Every score traces back to the metrics that produced it.' },
    { title: 'Publish', text: 'Share a live portfolio that grows as your code does.' },
  ];

  protected readonly dimensions = [
    {
      name: 'Activity',
      weight: '30%',
      text: 'Commit frequency, recency and consistency over the last two years.',
    },
    {
      name: 'Structure',
      weight: '30%',
      text: 'File sizes, folder depth and monolith indicators in the codebase.',
    },
    {
      name: 'Quality',
      weight: '40%',
      text: 'Tests, README, lint and CI configuration, plus commit hygiene.',
    },
  ];

  protected readonly jumpTo = jumpToFragment;

  protected signIn(): void {
    this.session.signIn('/dashboard');
  }
}

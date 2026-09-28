import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface FirstRunProgress {
  repositoryCount: number;
  selectedCount: number;
  analyzedCount: number;
  isPortfolioPublic: boolean;
}

export interface GuideStep {
  key: 'import' | 'select' | 'analyse' | 'view' | 'publish';
  title: string;
  text: string;
  done: boolean;
}

/** The onboarding path: import → select → analyse → view → publish. */
export function guideSteps(p: FirstRunProgress): GuideStep[] {
  return [
    {
      key: 'import',
      title: 'Import',
      text: 'Pull your repositories from GitHub.',
      done: p.repositoryCount > 0,
    },
    {
      key: 'select',
      title: 'Select',
      text: 'Choose the repositories that represent you.',
      done: p.selectedCount > 0,
    },
    {
      key: 'analyse',
      title: 'Analyse',
      text: 'Compute scores for your selection.',
      done: p.analyzedCount > 0,
    },
    {
      key: 'view',
      title: 'View',
      text: 'Explore scores, metrics and feedback.',
      done: p.analyzedCount > 0,
    },
    {
      key: 'publish',
      title: 'Publish',
      text: 'Make your portfolio public.',
      done: p.isPortfolioPublic,
    },
  ];
}

@Component({
  selector: 'app-first-run-guide',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="card guide" aria-labelledby="guide-title">
      <h2 id="guide-title">Get started — {{ doneCount() }} of {{ steps().length }} done</h2>
      <ol class="guide__steps list-reset">
        @for (step of steps(); track step.key; let i = $index) {
          <li class="guide__step" [class.is-done]="step.done" [class.is-next]="step === next()">
            <span class="guide__num" aria-hidden="true">{{ step.done ? '✓' : i + 1 }}</span>
            <div>
              <strong>{{ step.title }}</strong>
              <span class="visually-hidden">{{ step.done ? ' (done)' : '' }}</span>
              <p class="muted small">{{ step.text }}</p>
              @if (step === next()) {
                @switch (step.key) {
                  @case ('select') {
                    <a class="btn btn--sm btn--primary" routerLink="/repositories">Select repos</a>
                  }
                  @case ('publish') {
                    <a class="btn btn--sm btn--primary" routerLink="/settings">Open settings</a>
                  }
                  @default {
                    <span class="small muted">Use the buttons above.</span>
                  }
                }
              }
            </div>
          </li>
        }
      </ol>
    </section>
  `,
  styleUrl: './first-run-guide.scss',
})
export class FirstRunGuide {
  readonly progress = input.required<FirstRunProgress>();

  protected readonly steps = computed(() => guideSteps(this.progress()));
  protected readonly next = computed(() => this.steps().find((s) => !s.done) ?? null);
  protected readonly doneCount = computed(() => this.steps().filter((s) => s.done).length);
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Feedback, Severity } from '../../core/models/api.models';
import { FeedbackItem } from '../../shared/ui/feedback-item';

const SEVERITY_RANK: Record<Severity, number> = { high: 0, medium: 1, low: 2 };

function splitFeedback(items: readonly Feedback[]): {
  improvements: Feedback[];
  strengths: Feedback[];
} {
  return {
    improvements: items
      .filter((f) => !f.isStrength)
      .sort((a, b) => SEVERITY_RANK[a.severity] - SEVERITY_RANK[b.severity]),
    strengths: items.filter((f) => f.isStrength),
  };
}

@Component({
  selector: 'app-feedback-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FeedbackItem],
  template: `
    <div class="grid grid--2">
      <section aria-labelledby="fb-improve" class="stack">
        <h3 id="fb-improve" class="col-title">Improvements ({{ split().improvements.length }})</h3>
        @for (item of split().improvements; track item.id) {
          <app-feedback-item [feedback]="item" />
        } @empty {
          <p class="muted small">Nothing to improve here — well done.</p>
        }
      </section>
      <section aria-labelledby="fb-strength" class="stack">
        <h3 id="fb-strength" class="col-title">Strengths ({{ split().strengths.length }})</h3>
        @for (item of split().strengths; track item.id) {
          <app-feedback-item [feedback]="item" />
        } @empty {
          <p class="muted small">No strengths detected yet.</p>
        }
      </section>
    </div>
  `,
  styles: `
    .col-title {
      margin: 0;
      font-size: 1rem;
    }
  `,
})
export class FeedbackList {
  readonly feedback = input.required<Feedback[]>();

  protected readonly split = computed(() => splitFeedback(this.feedback()));
}

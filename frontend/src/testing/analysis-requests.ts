import { HttpTestingController } from '@angular/common/http/testing';
import { AnalysisRun, ScopeParam } from '../app/core/models/api.models';

type RunStartAnswer = AnalysisRun | { status: number; statusText: string };

function isHttpFailure(answer: RunStartAnswer): answer is { status: number; statusText: string } {
  return 'statusText' in answer;
}

/** Answers the two per-scope `POST /api/analysis/run/{repoId}` requests of one "Analyse" click. */
export function answerRunStarts(
  http: HttpTestingController,
  repoId: string,
  answer: (scope: ScopeParam) => RunStartAnswer,
): void {
  for (const scope of ['repo', 'user'] as const) {
    const request = http.expectOne(
      (r) => r.url === `/api/analysis/run/${repoId}` && r.params.get('scope') === scope,
    );
    const reply = answer(scope);
    if (isHttpFailure(reply)) {
      request.flush(null, reply);
    } else {
      request.flush(reply);
    }
  }
}

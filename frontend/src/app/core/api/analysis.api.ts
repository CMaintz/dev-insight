import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Analysis, AnalysisRun, ScopeParam, ScoreSnapshot } from '../models/api.models';

interface RepoScopeKey {
  repoId: string | undefined;
  scope: ScopeParam;
}

@Injectable({ providedIn: 'root' })
export class AnalysisApi {
  private readonly http = inject(HttpClient);

  run(repoId: string, scope: ScopeParam): Observable<AnalysisRun> {
    return this.http.post<AnalysisRun>(`/api/analysis/run/${encodeURIComponent(repoId)}`, null, {
      params: { scope },
    });
  }

  runAll(): Observable<AnalysisRun[]> {
    return this.http.post<AnalysisRun[]>('/api/analysis/run-all', null);
  }

  getRun(runId: string): Observable<AnalysisRun> {
    return this.http.get<AnalysisRun>(`/api/analysis/runs/${encodeURIComponent(runId)}`);
  }

  /** Latest analysis for a repo + scope. A 404 error means "never analysed". */
  latestResource(key: () => RepoScopeKey) {
    return httpResource<Analysis>(() => {
      const { repoId, scope } = key();
      return repoId
        ? { url: `/api/analysis/${encodeURIComponent(repoId)}`, params: { scope } }
        : undefined;
    });
  }

  historyResource(key: () => RepoScopeKey) {
    return httpResource<ScoreSnapshot[]>(
      () => {
        const { repoId, scope } = key();
        return repoId
          ? { url: `/api/analysis/${encodeURIComponent(repoId)}/history`, params: { scope } }
          : undefined;
      },
      { defaultValue: [] },
    );
  }
}

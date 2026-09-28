import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResult, Repository } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ReposApi {
  private readonly http = inject(HttpClient);

  /** Reactive list of all imported repositories. Call from an injection context. */
  listResource() {
    return httpResource<Repository[]>(() => '/api/repos', { defaultValue: [] });
  }

  /** Reactive single repository; `id()` returning undefined keeps the resource idle. */
  repoResource(id: () => string | undefined) {
    return httpResource<Repository>(() => {
      const value = id();
      return value ? `/api/repos/${encodeURIComponent(value)}` : undefined;
    });
  }

  importFromGitHub(): Observable<ImportResult> {
    return this.http.post<ImportResult>('/api/repos/import', null);
  }

  setSelected(id: string, isSelected: boolean): Observable<Repository> {
    return this.http.patch<Repository>(`/api/repos/${encodeURIComponent(id)}/select`, {
      isSelected,
    });
  }
}

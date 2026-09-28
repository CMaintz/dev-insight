import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Project, ProjectInput } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);

  listResource() {
    return httpResource<Project[]>(() => '/api/projects', { defaultValue: [] });
  }

  create(body: ProjectInput): Observable<Project> {
    return this.http.post<Project>('/api/projects', body);
  }

  update(id: string, body: ProjectInput): Observable<Project> {
    return this.http.put<Project>(`/api/projects/${encodeURIComponent(id)}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/projects/${encodeURIComponent(id)}`);
  }
}

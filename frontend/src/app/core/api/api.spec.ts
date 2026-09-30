import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, firstValueFrom } from 'rxjs';
import { aDashboard, aPortfolio, aProfile, aProject, aRepo, aRun } from '../../../testing/fixtures';
import { ScopeParam } from '../models/api.models';
import { AnalysisApi } from './analysis.api';
import { DashboardApi } from './dashboard.api';
import { PortfolioApi } from './portfolio.api';
import { ProfileApi } from './profile.api';
import { ProjectsApi } from './projects.api';
import { ReposApi } from './repos.api';

describe('API clients', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('ProfileApi reads, updates and logs out', async () => {
    const api = TestBed.inject(ProfileApi);
    const me = firstValueFrom(api.me());
    http.expectOne({ method: 'GET', url: '/api/me' }).flush(aProfile());
    expect((await me).login).toBe('octocat');

    const body = { bio: 'hi', linkedInUrl: null, isPortfolioPublic: true };
    const updated = firstValueFrom(api.update(body));
    const put = http.expectOne({ method: 'PUT', url: '/api/me/profile' });
    expect(put.request.body).toEqual(body);
    put.flush(aProfile({ bio: 'hi' }));
    expect((await updated).bio).toBe('hi');

    const out = firstValueFrom(api.logout());
    http
      .expectOne({ method: 'POST', url: '/api/auth/logout' })
      .flush(null, { status: 204, statusText: 'No Content' });
    await out;
  });

  it('ReposApi imports and toggles selection', async () => {
    const api = TestBed.inject(ReposApi);
    const imported = firstValueFrom(api.importFromGitHub());
    http
      .expectOne({ method: 'POST', url: '/api/repos/import' })
      .flush({ imported: 2, updated: 1, total: 3 });
    expect((await imported).total).toBe(3);

    const selected = firstValueFrom(api.setSelected('r 1', false));
    const patch = http.expectOne({ method: 'PATCH', url: '/api/repos/r%201/select' });
    expect(patch.request.body).toEqual({ isSelected: false });
    patch.flush(aRepo({ isSelected: false }));
    expect((await selected).isSelected).toBe(false);
  });

  it('ReposApi resources fetch the list and a single repository', async () => {
    const api = TestBed.inject(ReposApi);
    const id = signal<string | undefined>(undefined);
    const list = TestBed.runInInjectionContext(() => api.listResource());
    const one = TestBed.runInInjectionContext(() => api.repoResource(id));
    TestBed.tick();
    http.expectOne('/api/repos').flush([aRepo()]);
    http.expectNone((req) => req.url.startsWith('/api/repos/'));

    id.set('r1');
    TestBed.tick();
    http.expectOne('/api/repos/r1').flush(aRepo());
    await TestBed.inject(ApplicationRef).whenStable();
    expect(list.value()).toHaveLength(1);
    expect(one.value()?.id).toBe('r1');
  });

  it('AnalysisApi starts runs with the scope query parameter', async () => {
    const api = TestBed.inject(AnalysisApi);
    const run = firstValueFrom(api.run('r1', 'user'));
    const req = http.expectOne((r) => r.url === '/api/analysis/run/r1');
    expect(req.request.method).toBe('POST');
    expect(req.request.params.get('scope')).toBe('user');
    req.flush(aRun());
    expect((await run).id).toBe('run1');

    const all = firstValueFrom(api.runAll());
    http.expectOne({ method: 'POST', url: '/api/analysis/run-all' }).flush([aRun()]);
    expect(await all).toHaveLength(1);

    const polled = firstValueFrom(api.getRun('run1'));
    http.expectOne('/api/analysis/runs/run1').flush(aRun({ status: 'running' }));
    expect((await polled).status).toBe('running');
  });

  it('AnalysisApi resources follow repo id and scope', async () => {
    const api = TestBed.inject(AnalysisApi);
    const key = signal<{ repoId: string | undefined; scope: ScopeParam }>({
      repoId: 'r1',
      scope: 'repo',
    });
    TestBed.runInInjectionContext(() => api.latestResource(key));
    const history = TestBed.runInInjectionContext(() => api.historyResource(key));
    TestBed.tick();
    http
      .expectOne((r) => r.url === '/api/analysis/r1' && r.params.get('scope') === 'repo')
      .flush({});
    http
      .expectOne((r) => r.url === '/api/analysis/r1/history' && r.params.get('scope') === 'repo')
      .flush([]);
    await TestBed.inject(ApplicationRef).whenStable();
    expect(history.value()).toEqual([]);

    key.set({ repoId: undefined, scope: 'user' });
    TestBed.tick();
    http.expectNone(() => true);
  });

  it('DashboardApi requests the selected scope', async () => {
    const scope = signal<ScopeParam>('user');
    const res = TestBed.runInInjectionContext(() => TestBed.inject(DashboardApi).resource(scope));
    TestBed.tick();
    http
      .expectOne((r) => r.url === '/api/dashboard' && r.params.get('scope') === 'user')
      .flush(aDashboard());
    await TestBed.inject(ApplicationRef).whenStable();
    expect(res.value()?.repositoryCount).toBe(2);
  });

  const PROJECT_BODY = {
    name: 'x',
    description: null,
    imageUrls: [],
    linkedRepositoryIds: [],
    sortOrder: 0,
  };

  function answer<T>(
    request: Observable<T>,
    match: { method: string; url: string },
    body: Parameters<TestRequest['flush']>[0],
    options?: Parameters<TestRequest['flush']>[1],
  ): Promise<T> {
    const result = firstValueFrom(request);
    http.expectOne(match).flush(body, options);
    return result;
  }

  it('ProjectsApi creates a project', async () => {
    const api = TestBed.inject(ProjectsApi);
    const created = await answer(
      api.create(PROJECT_BODY),
      { method: 'POST', url: '/api/projects' },
      aProject(),
      { status: 201, statusText: 'Created' },
    );
    expect(created.id).toBe('p1');
  });

  it('ProjectsApi updates a project', async () => {
    const api = TestBed.inject(ProjectsApi);
    const put = { method: 'PUT', url: '/api/projects/p1' };
    expect((await answer(api.update('p1', PROJECT_BODY), put, aProject())).id).toBe('p1');
  });

  it('ProjectsApi deletes a project', async () => {
    const api = TestBed.inject(ProjectsApi);
    const del = { method: 'DELETE', url: '/api/projects/p1' };
    await answer(api.delete('p1'), del, null, { status: 204, statusText: 'No Content' });
  });

  it('ProjectsApi lists projects as a resource', async () => {
    const list = TestBed.runInInjectionContext(() => TestBed.inject(ProjectsApi).listResource());
    TestBed.tick();
    http.expectOne('/api/projects').flush([aProject()]);
    await TestBed.inject(ApplicationRef).whenStable();
    expect(list.value()).toHaveLength(1);
  });

  it('PortfolioApi encodes the handle', () => {
    const handle = signal('octo cat');
    TestBed.runInInjectionContext(() => TestBed.inject(PortfolioApi).resource(handle));
    TestBed.tick();
    http.expectOne('/api/portfolio/octo%20cat').flush(aPortfolio());
  });
});

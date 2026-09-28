import { aDashboard, aRun } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { DashboardPage } from './dashboard.page';

describe('DashboardPage', () => {
  it('shows scores, KPIs, the repository table and top feedback', async () => {
    const { http, element, settle } = createPage(DashboardPage);
    await settle();
    http
      .expectOne((r) => r.url === '/api/dashboard' && r.params.get('scope') === 'repo')
      .flush(aDashboard());
    await settle();

    const content = text(element);
    expect(element.querySelector('[aria-label="Overall: 70 out of 100, Fair"]')).not.toBeNull();
    expect(content).toContain('Vague commits');
    expect(content).toContain('20%');
    expect(element.querySelector('app-repo-score-table')?.textContent).toContain('alpha');
    expect(content).toContain('Large files');
    expect(content).toContain('Get started — 4 of 5 done');
    http.verify();
  });

  it('guides first-time users to import', async () => {
    const { http, element, settle } = createPage(DashboardPage);
    await settle();
    http
      .expectOne((r) => r.url === '/api/dashboard')
      .flush(
        aDashboard({
          repositoryCount: 0,
          selectedCount: 0,
          analyzedCount: 0,
          averageScores: null,
          repositories: [],
        }),
      );
    await settle();

    expect(text(element)).toContain('No repositories yet');
    button(element, 'Import from GitHub').click();
    http
      .expectOne({ method: 'POST', url: '/api/repos/import' })
      .flush({ imported: 3, updated: 0, total: 3 });
    await settle();
    // Successful import refetches the dashboard.
    http.expectOne((r) => r.url === '/api/dashboard').flush(aDashboard());
    await settle();
    expect(text(element)).toContain('Repositories');
  });

  it('re-requests the dashboard when the scope changes', async () => {
    const { http, element, settle } = createPage(DashboardPage);
    await settle();
    http.expectOne((r) => r.url === '/api/dashboard').flush(aDashboard());
    await settle();

    element.querySelectorAll<HTMLInputElement>('input[type="radio"]')[1].click();
    await settle();
    http
      .expectOne((r) => r.url === '/api/dashboard' && r.params.get('scope') === 'user')
      .flush(aDashboard());
  });

  it('starts analysis for all selected repositories', async () => {
    const { http, element, settle } = createPage(DashboardPage);
    await settle();
    http.expectOne((r) => r.url === '/api/dashboard').flush(aDashboard());
    await settle();

    button(element, 'Analyse all selected').click();
    http.expectOne({ method: 'POST', url: '/api/analysis/run-all' }).flush([aRun()]);
    await settle();
    expect(text(element)).toContain('1 analysis runs in progress');
  });

  it('offers a retry when loading fails', async () => {
    const { http, element, settle } = createPage(DashboardPage);
    await settle();
    http.expectOne((r) => r.url === '/api/dashboard').flush(null, { status: 500, statusText: 'x' });
    await settle();
    expect(text(element)).toContain('Could not load the dashboard');
  });
});

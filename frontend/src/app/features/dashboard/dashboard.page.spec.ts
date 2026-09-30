import { HttpRequest } from '@angular/common/http';
import { Dashboard } from '../../core/models/api.models';
import { aDashboard, aRun } from '../../../testing/fixtures';
import { openPage, text } from '../../../testing/page-harness';
import { DashboardPage } from './dashboard.page';

const isDashboardRequest = (scope: string) => (r: HttpRequest<unknown>) =>
  r.url === '/api/dashboard' && r.params.get('scope') === scope;

function openDashboard(dashboard: Dashboard | null = aDashboard()) {
  return openPage(DashboardPage, [[isDashboardRequest('repo'), dashboard]]);
}

function aFirstRunDashboard(): Dashboard {
  return aDashboard({
    repositoryCount: 0,
    selectedCount: 0,
    analyzedCount: 0,
    averageScores: null,
    repositories: [],
  });
}

describe('DashboardPage', () => {
  it('shows the average scores, KPIs, repository table and top feedback', async () => {
    const { element } = await openDashboard();
    const content = text(element);
    expect(element.querySelector('[aria-label="Overall: 70 out of 100, Fair"]')).not.toBeNull();
    expect(content).toContain('Vague commits');
    expect(content).toContain('20%');
    expect(element.querySelector('app-repo-score-table')?.textContent).toContain('alpha');
    expect(content).toContain('Large files');
  });

  it('tracks first-run progress in the guide', async () => {
    const { element } = await openDashboard();
    expect(text(element)).toContain('Get started — 4 of 5 done');
  });

  it('guides first-time users to import', async () => {
    const { element } = await openDashboard(aFirstRunDashboard());
    expect(text(element)).toContain('No repositories yet');
  });

  it('refetches the dashboard after a successful import', async () => {
    const page = await openDashboard(aFirstRunDashboard());
    await page.click('Import from GitHub');
    await page.respond(
      { method: 'POST', url: '/api/repos/import' },
      { imported: 3, updated: 0, total: 3 },
    );
    await page.respond((r) => r.url === '/api/dashboard', aDashboard());
    expect(text(page.element)).not.toContain('No repositories yet');
  });

  it('re-requests the dashboard when the scope changes', async () => {
    const page = await openDashboard();
    page.element.querySelectorAll<HTMLInputElement>('input[type="radio"]')[1].click();
    await page.settle();
    page.http.expectOne(isDashboardRequest('user')).flush(aDashboard());
  });

  it('starts analysis for all selected repositories', async () => {
    const page = await openDashboard();
    await page.click('Analyse all selected');
    await page.respond({ method: 'POST', url: '/api/analysis/run-all' }, [aRun()]);
    expect(text(page.element)).toContain('1 analysis runs in progress');
  });

  it('offers a retry when loading fails', async () => {
    const page = await openPage(DashboardPage, [
      [isDashboardRequest('repo'), null, { status: 500, statusText: 'x' }],
    ]);
    expect(text(page.element)).toContain('Could not load the dashboard');
  });
});

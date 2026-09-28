import { TestBed } from '@angular/core/testing';
import { ToastService } from '../../core/notifications/toast.service';
import { aRepo, aRun } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { RepositoriesPage } from './repositories.page';

const repos = [
  aRepo({ id: 'r1', name: 'alpha', language: 'TypeScript', isSelected: true }),
  aRepo({ id: 'r2', name: 'beta', language: 'Kotlin', isSelected: false, isFork: true }),
];

async function loaded() {
  const page = createPage(RepositoriesPage);
  await page.settle();
  page.http.expectOne('/api/repos').flush(repos);
  await page.settle();
  return page;
}

describe('RepositoriesPage', () => {
  it('shows a skeleton, not the empty state, while the first load is in flight', async () => {
    const page = createPage(RepositoriesPage);
    await page.settle();
    expect(page.element.querySelector('.skeleton')).not.toBeNull();
    expect(text(page.element)).not.toContain('No repositories imported');
    page.http.expectOne('/api/repos').flush(repos);
    await page.settle();
    expect(page.element.querySelector('.skeleton')).toBeNull();
  });

  it('lists repositories with facts and a selection count', async () => {
    const { element } = await loaded();
    const content = text(element);
    expect(content).toContain('alpha');
    expect(content).toContain('beta');
    expect(content).toContain('fork');
    expect(content).toContain('Showing 2 of 2 repositories · 1 selected');
    expect(button(element, 'Analyse all selected').textContent).toContain('(1)');
  });

  it('filters by search text, language and selection', async () => {
    const { element, settle } = await loaded();
    const search = element.querySelector<HTMLInputElement>('#repo-search');
    if (!search) throw new Error('missing search');
    search.value = 'bet';
    search.dispatchEvent(new Event('input'));
    await settle();
    expect(text(element)).toContain('Showing 1 of 2');

    search.value = '';
    search.dispatchEvent(new Event('input'));
    const language = element.querySelector<HTMLSelectElement>('#repo-language');
    if (!language) throw new Error('missing select');
    language.value = 'Kotlin';
    language.dispatchEvent(new Event('change'));
    await settle();
    expect(text(element)).toContain('Showing 1 of 2');

    language.value = '';
    language.dispatchEvent(new Event('change'));
    const selectedOnly = element.querySelector<HTMLInputElement>('.filters input[type="checkbox"]');
    selectedOnly?.click();
    await settle();
    expect(text(element)).toContain('Showing 1 of 2');

    search.value = 'zzz';
    search.dispatchEvent(new Event('input'));
    await settle();
    expect(text(element)).toContain('No repositories match these filters.');
  });

  it('toggles selection with an optimistic update and rolls back on error', async () => {
    const { element, http, settle } = await loaded();
    const checkbox = element.querySelector<HTMLInputElement>('#select-r2');
    checkbox?.click();
    await settle();
    expect(text(element)).toContain('2 selected');
    const patch = http.expectOne({ method: 'PATCH', url: '/api/repos/r2/select' });
    expect(patch.request.body).toEqual({ isSelected: true });
    patch.flush(null, { status: 500, statusText: 'x' });
    await settle();
    expect(text(element)).toContain('1 selected');

    element.querySelector<HTMLInputElement>('#select-r1')?.click();
    http.expectOne('/api/repos/r1/select').flush({ ...repos[0], isSelected: false });
    await settle();
    expect(text(element)).toContain('0 selected');
  });

  it('analyses a repository in both scopes and shows progress', async () => {
    const { element, http, settle } = await loaded();
    element.querySelector<HTMLButtonElement>('button[aria-label="Analyse alpha"]')?.click();
    await settle();
    http
      .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === 'repo')
      .flush(aRun({ id: 'x1' }));
    http
      .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === 'user')
      .flush(aRun({ id: 'x2', scope: 'userContribution' }));
    await settle();
    expect(text(element)).toContain('Queued (0/2)');
  });

  it('announces a finished analysis while keeping "View results" a plain link', async () => {
    const { element, http, settle } = await loaded();
    element.querySelector<HTMLButtonElement>('button[aria-label="Analyse alpha"]')?.click();
    await settle();
    for (const scope of ['repo', 'user']) {
      http
        .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === scope)
        .flush(aRun({ id: `done-${scope}`, status: 'succeeded' }));
    }
    await settle();
    const link = [...element.querySelectorAll('a')].find((a) =>
      a.textContent?.includes('View results'),
    );
    expect(link?.getAttribute('role')).toBeNull();
    expect(link?.closest('[role="status"]')).not.toBeNull();
  });

  it('explains when the repository to analyse no longer exists', async () => {
    const { element, http, settle } = await loaded();
    element.querySelector<HTMLButtonElement>('button[aria-label="Analyse alpha"]')?.click();
    await settle();
    for (const scope of ['repo', 'user']) {
      http
        .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === scope)
        .flush(null, { status: 404, statusText: 'Not Found' });
    }
    await settle();
    expect(TestBed.inject(ToastService).toasts()[0]).toMatchObject({
      title: 'Not found',
      detail: 'That repository no longer exists — re-import from GitHub.',
    });
  });

  it('shows an import prompt when nothing is imported', async () => {
    const page = createPage(RepositoriesPage);
    await page.settle();
    page.http.expectOne('/api/repos').flush([]);
    await page.settle();
    expect(text(page.element)).toContain('No repositories imported');
  });
});

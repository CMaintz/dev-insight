import { TestBed } from '@angular/core/testing';
import { ToastService } from '../../core/notifications/toast.service';
import { answerRunStarts } from '../../../testing/analysis-requests';
import { aRepo, aRun } from '../../../testing/fixtures';
import {
  NOT_FOUND,
  button,
  createPage,
  openPage,
  requireElement,
  text,
} from '../../../testing/page-harness';
import { RepositoriesPage } from './repositories.page';

const repos = [
  aRepo({ id: 'r1', name: 'alpha', language: 'TypeScript', isSelected: true }),
  aRepo({ id: 'r2', name: 'beta', language: 'Kotlin', isSelected: false, isFork: true }),
];

type RepositoriesHarness = Awaited<ReturnType<typeof loaded>>;

function loaded() {
  return openPage(RepositoriesPage, [['/api/repos', repos]]);
}

async function search(page: RepositoriesHarness, query: string): Promise<void> {
  const field = requireElement<HTMLInputElement>(page.element, '#repo-search');
  field.value = query;
  field.dispatchEvent(new Event('input'));
  await page.settle();
}

async function chooseLanguage(page: RepositoriesHarness, language: string): Promise<void> {
  const select = requireElement<HTMLSelectElement>(page.element, '#repo-language');
  select.value = language;
  select.dispatchEvent(new Event('change'));
  await page.settle();
}

async function analyseAlpha(page: RepositoriesHarness): Promise<void> {
  page.element.querySelector<HTMLButtonElement>('button[aria-label="Analyse alpha"]')?.click();
  await page.settle();
}

describe('RepositoriesPage', () => {
  it('shows a skeleton, not the empty state, while the first load is in flight', async () => {
    const page = createPage(RepositoriesPage);
    await page.settle();
    expect(page.element.querySelector('.skeleton')).not.toBeNull();
    expect(text(page.element)).not.toContain('No repositories imported');
    await page.respond('/api/repos', repos);
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

  it('searches names and descriptions', async () => {
    const page = await loaded();
    await search(page, 'bet');
    expect(text(page.element)).toContain('Showing 1 of 2');
    await search(page, 'zzz');
    expect(text(page.element)).toContain('No repositories match these filters.');
  });

  it('filters by language', async () => {
    const page = await loaded();
    await chooseLanguage(page, 'Kotlin');
    expect(text(page.element)).toContain('Showing 1 of 2');
  });

  it('filters to selected repositories', async () => {
    const page = await loaded();
    page.element.querySelector<HTMLInputElement>('.filters input[type="checkbox"]')?.click();
    await page.settle();
    expect(text(page.element)).toContain('Showing 1 of 2');
  });

  it('toggles selection optimistically and rolls back on error', async () => {
    const page = await loaded();
    page.element.querySelector<HTMLInputElement>('#select-r2')?.click();
    await page.settle();
    expect(text(page.element)).toContain('2 selected');
    const patch = page.http.expectOne({ method: 'PATCH', url: '/api/repos/r2/select' });
    expect(patch.request.body).toEqual({ isSelected: true });
    patch.flush(null, { status: 500, statusText: 'x' });
    await page.settle();
    expect(text(page.element)).toContain('1 selected');
  });

  it('keeps the server answer after a successful toggle', async () => {
    const page = await loaded();
    page.element.querySelector<HTMLInputElement>('#select-r1')?.click();
    await page.respond('/api/repos/r1/select', { ...repos[0], isSelected: false });
    expect(text(page.element)).toContain('0 selected');
  });

  it('analyses a repository in both scopes and shows progress', async () => {
    const page = await loaded();
    await analyseAlpha(page);
    answerRunStarts(page.http, 'r1', (scope) => aRun({ id: `x-${scope}` }));
    await page.settle();
    expect(text(page.element)).toContain('Queued (0/2)');
  });

  it('announces a finished analysis while keeping "View results" a plain link', async () => {
    const page = await loaded();
    await analyseAlpha(page);
    answerRunStarts(page.http, 'r1', (scope) => aRun({ id: `done-${scope}`, status: 'succeeded' }));
    await page.settle();
    const link = [...page.element.querySelectorAll('a')].find((a) =>
      a.textContent?.includes('View results'),
    );
    expect(link?.getAttribute('role')).toBeNull();
    expect(link?.closest('[role="status"]')).not.toBeNull();
  });

  it('explains when the repository to analyse no longer exists', async () => {
    const page = await loaded();
    await analyseAlpha(page);
    answerRunStarts(page.http, 'r1', () => NOT_FOUND);
    await page.settle();
    expect(TestBed.inject(ToastService).toasts()[0]).toMatchObject({
      title: 'Not found',
      detail: 'That repository no longer exists — re-import from GitHub.',
    });
  });

  it('shows an import prompt when nothing is imported', async () => {
    const { element } = await openPage(RepositoriesPage, [['/api/repos', []]]);
    expect(text(element)).toContain('No repositories imported');
  });
});

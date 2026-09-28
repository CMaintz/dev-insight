import { aProject, aRepo } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { createProjectForm, nextSortOrder, toProjectInput } from './project-form.model';
import { ProjectsPage } from './projects.page';

async function loaded(projects = [aProject()]) {
  const page = createPage(ProjectsPage);
  await page.settle();
  page.http.expectOne('/api/projects').flush(projects);
  page.http.expectOne('/api/repos').flush([aRepo()]);
  await page.settle();
  return page;
}

function type(element: HTMLElement, selector: string, value: string) {
  const input = element.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
  if (!input) throw new Error(`missing ${selector}`);
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

describe('ProjectsPage', () => {
  it('shows a skeleton, not the empty state, while the first load is in flight', async () => {
    const page = createPage(ProjectsPage);
    await page.settle();
    expect(page.element.querySelector('.skeleton')).not.toBeNull();
    expect(text(page.element)).not.toContain('No projects yet');
    page.http.expectOne('/api/projects').flush([]);
    page.http.expectOne('/api/repos').flush([]);
  });

  it('lists projects with their linked repositories', async () => {
    const { element } = await loaded();
    expect(text(element)).toContain('Portfolio site');
    expect(text(element)).toContain('alpha');
  });

  it('validates and creates a project', async () => {
    const { element, http, settle } = await loaded([]);
    expect(text(element)).toContain('No projects yet');
    button(element, 'New project').click();
    await settle();

    button(element, 'Save project').click();
    await settle();
    expect(text(element)).toContain('A name is required.');

    type(element, '#project-name', '  Shiny thing ');
    button(element, 'Add image URL').click();
    await settle();
    type(element, '#image-0', 'http://insecure.example/a.png');
    button(element, 'Save project').click();
    await settle();
    expect(text(element)).toContain('Enter a full https:// URL.');
    http.expectNone('/api/projects');

    type(element, '#image-0', 'https://cdn.example/a.png');
    element.querySelector<HTMLInputElement>('.repo-list input')?.click();
    button(element, 'Save project').click();
    await settle();

    const post = http.expectOne({ method: 'POST', url: '/api/projects' });
    expect(post.request.body).toEqual({
      name: 'Shiny thing',
      description: null,
      imageUrls: ['https://cdn.example/a.png'],
      linkedRepositoryIds: ['r1'],
      sortOrder: 0,
    });
    post.flush(aProject({ id: 'new', name: 'Shiny thing' }));
    await settle();
    expect(text(element)).toContain('Shiny thing');
    expect(element.querySelector('app-project-form')).toBeNull();
  });

  it('rebuilds the form when switching the edit target from one project to another', async () => {
    const projectA = aProject({ id: 'a', name: 'Alpha site', sortOrder: 0 });
    const projectB = aProject({ id: 'b', name: 'Beta app', description: 'B desc', sortOrder: 1 });
    const { element, http, settle } = await loaded([projectA, projectB]);

    const editButtons = () =>
      [...element.querySelectorAll<HTMLButtonElement>('.project__actions button')].filter(
        (b) => b.textContent?.trim() === 'Edit',
      );
    editButtons()[0].click();
    await settle();
    expect(element.querySelector<HTMLInputElement>('#project-name')?.value).toBe('Alpha site');

    editButtons()[1].click();
    await settle();
    expect(element.querySelector<HTMLInputElement>('#project-name')?.value).toBe('Beta app');
    expect(element.querySelector<HTMLTextAreaElement>('#project-description')?.value).toBe(
      'B desc',
    );

    button(element, 'Save project').click();
    await settle();
    const put = http.expectOne({ method: 'PUT', url: '/api/projects/b' });
    expect(put.request.body).toMatchObject({
      name: 'Beta app',
      description: 'B desc',
      sortOrder: 1,
    });
    put.flush(projectB);
    await settle();
  });

  it('disables deleting while a project is being edited', async () => {
    const { element, settle } = await loaded();
    button(element, 'Edit').click();
    await settle();
    expect(button(element, 'Delete').disabled).toBe(true);
  });

  it('rejects whitespace-only names and over-long descriptions inline', async () => {
    const { element, http, settle } = await loaded([]);
    button(element, 'New project').click();
    await settle();
    type(element, '#project-name', '   ');
    type(element, '#project-description', 'x'.repeat(4001));
    button(element, 'Save project').click();
    await settle();

    expect(text(element)).toContain('A name is required.');
    expect(text(element)).toContain('Keep the description under 4000 characters.');
    http.expectNone('/api/projects');
  });

  it('edits an existing project', async () => {
    const { element, http, settle } = await loaded();
    button(element, 'Edit').click();
    await settle();
    expect(element.querySelector<HTMLInputElement>('#project-name')?.value).toBe('Portfolio site');
    element.querySelector<HTMLButtonElement>('button[aria-label="Remove image 1"]')?.click();
    button(element, 'Save project').click();
    await settle();
    const put = http.expectOne({ method: 'PUT', url: '/api/projects/p1' });
    expect(put.request.body.imageUrls).toEqual([]);
    put.flush(aProject({ imageUrls: [] }));
    await settle();
  });

  it('asks for confirmation before deleting', async () => {
    const { element, http, settle } = await loaded();
    button(element, 'Delete').click();
    await settle();
    http.expectNone('/api/projects/p1');
    button(element, 'Confirm delete').click();
    http
      .expectOne({ method: 'DELETE', url: '/api/projects/p1' })
      .flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    expect(text(element)).toContain('No projects yet');
  });
});

describe('project form model', () => {
  it('normalises the form value', () => {
    expect(
      toProjectInput({
        name: ' x ',
        description: '  ',
        imageUrls: [' https://a.io/1.png ', ''],
        linkedRepositoryIds: ['r1'],
        sortOrder: 3,
      }),
    ).toEqual({
      name: 'x',
      description: null,
      imageUrls: ['https://a.io/1.png'],
      linkedRepositoryIds: ['r1'],
      sortOrder: 3,
    });
  });

  it('rejects negative or fractional sort orders', () => {
    const form = createProjectForm(null);
    form.controls.name.setValue('ok');
    form.controls.sortOrder.setValue(-1);
    expect(form.valid).toBe(false);
    form.controls.sortOrder.setValue(1.5);
    expect(form.valid).toBe(false);
    form.controls.sortOrder.setValue(2);
    expect(form.valid).toBe(true);
  });

  it('suggests the next sort position', () => {
    expect(nextSortOrder([])).toBe(0);
    expect(nextSortOrder([aProject({ sortOrder: 4 }), aProject({ sortOrder: 1 })])).toBe(5);
  });
});

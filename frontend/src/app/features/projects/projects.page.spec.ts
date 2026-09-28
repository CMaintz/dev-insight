import { aProject, aRepo } from '../../../testing/fixtures';
import { button, createPage, text, typeInto } from '../../../testing/page-harness';
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

type ProjectsHarness = Awaited<ReturnType<typeof loaded>>;

async function openNewProjectForm(): Promise<ProjectsHarness> {
  const page = await loaded([]);
  await page.click('New project');
  return page;
}

async function addImage(page: ProjectsHarness, url: string): Promise<void> {
  await page.click('Add image URL');
  typeInto(page.element, '#image-0', url);
}

async function editCard(page: ProjectsHarness, index: number): Promise<void> {
  const editButtons = [
    ...page.element.querySelectorAll<HTMLButtonElement>('.project__actions button'),
  ].filter((b) => b.textContent?.trim() === 'Edit');
  editButtons[index].click();
  await page.settle();
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

  it('requires a name and https image URLs before saving', async () => {
    const page = await openNewProjectForm();
    await page.click('Save project');
    expect(text(page.element)).toContain('A name is required.');

    await addImage(page, 'http://insecure.example/a.png');
    await page.click('Save project');
    expect(text(page.element)).toContain('Enter a full https:// URL.');
    page.http.expectNone('/api/projects');
  });

  it('creates a project with a trimmed name, images and linked repositories', async () => {
    const page = await openNewProjectForm();
    typeInto(page.element, '#project-name', '  Shiny thing ');
    await addImage(page, 'https://cdn.example/a.png');
    page.element.querySelector<HTMLInputElement>('.repo-list input')?.click();
    await page.click('Save project');

    expect(page.http.expectOne({ method: 'POST', url: '/api/projects' }).request.body).toEqual({
      name: 'Shiny thing',
      description: null,
      imageUrls: ['https://cdn.example/a.png'],
      linkedRepositoryIds: ['r1'],
      sortOrder: 0,
    });
  });

  it('closes the form and lists the project once it is created', async () => {
    const page = await openNewProjectForm();
    typeInto(page.element, '#project-name', 'Shiny thing');
    await page.click('Save project');
    await page.respond('/api/projects', aProject({ id: 'new', name: 'Shiny thing' }));
    expect(text(page.element)).toContain('Shiny thing');
    expect(page.element.querySelector('app-project-form')).toBeNull();
  });

  it('rebuilds the form when switching the edit target from one project to another', async () => {
    const projectA = aProject({ id: 'a', name: 'Alpha site', sortOrder: 0 });
    const projectB = aProject({ id: 'b', name: 'Beta app', description: 'B desc', sortOrder: 1 });
    const page = await loaded([projectA, projectB]);

    await editCard(page, 0);
    expect(page.fieldValue('#project-name')).toBe('Alpha site');
    await editCard(page, 1);
    expect(page.fieldValue('#project-name')).toBe('Beta app');
    expect(page.fieldValue('#project-description')).toBe('B desc');

    await page.click('Save project');
    const put = page.http.expectOne({ method: 'PUT', url: '/api/projects/b' });
    expect(put.request.body).toMatchObject({
      name: 'Beta app',
      description: 'B desc',
      sortOrder: 1,
    });
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
    typeInto(element, '#project-name', '   ');
    typeInto(element, '#project-description', 'x'.repeat(4001));
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

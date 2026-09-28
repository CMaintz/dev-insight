import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { Project, ProjectInput } from '../../core/models/api.models';
import { httpsUrlValidator } from '../../shared/forms/validators';

export const NAME_MAX = 200;
export const DESCRIPTION_MAX = 2000;
export const MAX_IMAGES = 10;

export type ProjectFormGroup = FormGroup<{
  name: FormControl<string>;
  description: FormControl<string>;
  imageUrls: FormArray<FormControl<string>>;
  linkedRepositoryIds: FormControl<string[]>;
  sortOrder: FormControl<number>;
}>;

export function imageUrlControl(value = ''): FormControl<string> {
  return new FormControl(value, {
    nonNullable: true,
    validators: [Validators.required, httpsUrlValidator],
  });
}

export function createProjectForm(project: Project | null, nextSortOrder = 0): ProjectFormGroup {
  return new FormGroup({
    name: new FormControl(project?.name ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(NAME_MAX)],
    }),
    description: new FormControl(project?.description ?? '', {
      nonNullable: true,
      validators: [Validators.maxLength(DESCRIPTION_MAX)],
    }),
    imageUrls: new FormArray((project?.imageUrls ?? []).map((url) => imageUrlControl(url))),
    linkedRepositoryIds: new FormControl<string[]>(project?.linkedRepositoryIds ?? [], {
      nonNullable: true,
    }),
    sortOrder: new FormControl(project?.sortOrder ?? nextSortOrder, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)],
    }),
  });
}

/** Normalises the raw form value into the API request body. */
export function toProjectInput(value: ProjectFormGroup['value']): ProjectInput {
  const description = value.description?.trim() ?? '';
  return {
    name: value.name?.trim() ?? '',
    description: description.length > 0 ? description : null,
    imageUrls: (value.imageUrls ?? []).map((url) => url.trim()).filter((url) => url.length > 0),
    linkedRepositoryIds: value.linkedRepositoryIds ?? [],
    sortOrder: Number(value.sortOrder ?? 0),
  };
}

/** Next free sort position: one past the current maximum. */
export function nextSortOrder(projects: readonly Project[]): number {
  return projects.reduce((max, p) => Math.max(max, p.sortOrder + 1), 0);
}

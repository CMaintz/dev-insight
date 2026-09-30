import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Project, ProjectInput, Repository } from '../../core/models/api.models';
import {
  DESCRIPTION_MAX,
  MAX_IMAGES,
  NAME_MAX,
  ProjectFormGroup,
  createProjectForm,
  imageUrlControl,
  toProjectInput,
} from './project-form.model';

@Component({
  selector: 'app-project-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './project-form.html',
  styleUrl: './project-form.scss',
})
export class ProjectForm implements OnInit {
  readonly project = input<Project | null>(null);
  readonly repositories = input<Repository[]>([]);
  readonly defaultSortOrder = input(0);
  readonly saving = input(false);

  readonly save = output<ProjectInput>();
  readonly cancelled = output<void>();

  protected form: ProjectFormGroup = createProjectForm(null);
  protected readonly submitted = signal(false);
  protected readonly nameMax = NAME_MAX;
  protected readonly descriptionMax = DESCRIPTION_MAX;
  protected readonly maxImages = MAX_IMAGES;
  protected readonly heading = computed(() => (this.project() ? 'Edit project' : 'New project'));

  ngOnInit(): void {
    this.form = createProjectForm(this.project(), this.defaultSortOrder());
  }

  protected get imageUrls() {
    return this.form.controls.imageUrls;
  }

  protected addImage(): void {
    if (this.imageUrls.length < MAX_IMAGES) {
      this.imageUrls.push(imageUrlControl());
    }
  }

  protected removeImage(index: number): void {
    this.imageUrls.removeAt(index);
  }

  protected isLinked(id: string): boolean {
    return this.form.controls.linkedRepositoryIds.value.includes(id);
  }

  protected toggleRepo(id: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const control = this.form.controls.linkedRepositoryIds;
    const without = control.value.filter((value) => value !== id);
    control.setValue(checked ? [...without, id] : without);
    control.markAsDirty();
  }

  protected showError(control: { invalid: boolean; touched: boolean }): boolean {
    return control.invalid && (control.touched || this.submitted());
  }

  protected submit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();
    if (this.form.valid) {
      this.save.emit(toProjectInput(this.form.getRawValue()));
    }
  }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProfileApi } from '../../core/api/profile.api';
import { UserActionErrors } from '../../core/http/surfaced-errors';
import { SessionStore } from '../../core/auth/session.store';
import { Profile, ProfileUpdate } from '../../core/models/api.models';
import { ToastService } from '../../core/notifications/toast.service';
import { linkedInUrlValidator } from '../../shared/forms/validators';
import { BusyFlag } from '../../shared/util/busy-flag';

export const BIO_MAX = 2000;

export function createProfileForm(profile: Profile | null) {
  return new FormGroup({
    bio: new FormControl(profile?.bio ?? '', {
      nonNullable: true,
      validators: [Validators.maxLength(BIO_MAX)],
    }),
    linkedInUrl: new FormControl(profile?.linkedInUrl ?? '', {
      nonNullable: true,
      validators: [linkedInUrlValidator],
    }),
    isPortfolioPublic: new FormControl(profile?.isPortfolioPublic ?? false, { nonNullable: true }),
  });
}

export function toProfileUpdate(value: {
  bio: string;
  linkedInUrl: string;
  isPortfolioPublic: boolean;
}): ProfileUpdate {
  const bio = value.bio.trim();
  const linkedInUrl = value.linkedInUrl.trim();
  return {
    bio: bio.length > 0 ? bio : null,
    linkedInUrl: linkedInUrl.length > 0 ? linkedInUrl : null,
    isPortfolioPublic: value.isPortfolioPublic,
  };
}

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss',
})
export class SettingsPage {
  private readonly api = inject(ProfileApi);
  private readonly toasts = inject(ToastService);
  private readonly errors = inject(UserActionErrors);
  protected readonly session = inject(SessionStore);

  protected readonly form = createProfileForm(this.session.profile());
  private readonly saveFlag = new BusyFlag();
  protected readonly saving = this.saveFlag.active;
  protected readonly bioMax = BIO_MAX;
  protected readonly portfolioPath = computed(() => this.session.profile()?.portfolioPath ?? null);

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    const profile = await this.saveFlag.run(() =>
      this.errors.resultOrNothing(
        this.api.update(toProfileUpdate(this.form.getRawValue())),
        'Your account was not found — please sign in again.',
      ),
    );
    if (profile) {
      this.session.setProfile(profile);
      this.form.markAsPristine();
      this.toasts.success('Profile saved');
    }
  }
}

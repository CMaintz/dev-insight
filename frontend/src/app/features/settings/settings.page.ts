import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/profile.api';
import { SessionStore } from '../../core/auth/session.store';
import { Profile, ProfileUpdate } from '../../core/models/api.models';
import { ToastService } from '../../core/notifications/toast.service';
import { linkedInUrlValidator } from '../../shared/forms/validators';

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
  protected readonly session = inject(SessionStore);

  protected readonly form = createProfileForm(this.session.profile());
  protected readonly saving = signal(false);
  protected readonly bioMax = BIO_MAX;
  protected readonly portfolioPath = computed(() => this.session.profile()?.portfolioPath ?? null);

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    try {
      const profile = await firstValueFrom(
        this.api.update(toProfileUpdate(this.form.getRawValue())),
      );
      this.session.setProfile(profile);
      this.form.markAsPristine();
      this.toasts.success('Profile saved');
    } catch {
      // Surfaced by the interceptor.
    } finally {
      this.saving.set(false);
    }
  }
}

import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { aProfile } from '../../../testing/fixtures';
import { Profile } from '../models/api.models';
import { authGuard } from './auth.guard';
import { SessionStore } from './session.store';

function runGuard(profile: Profile | null) {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: SessionStore, useValue: { ensureLoaded: () => Promise.resolve(profile) } },
    ],
  });
  return TestBed.runInInjectionContext(() =>
    authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );
}

describe('authGuard', () => {
  it('allows signed-in users', async () => {
    expect(await runGuard(aProfile())).toBe(true);
  });

  it('redirects anonymous users to the landing page', async () => {
    const result = await runGuard(null);
    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/');
  });
});

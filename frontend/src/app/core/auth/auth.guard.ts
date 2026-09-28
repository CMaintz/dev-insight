import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

/** Lets signed-in users through; everyone else lands on the landing page. */
export const authGuard: CanActivateFn = async () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const profile = await session.ensureLoaded();
  return profile ? true : router.createUrlTree(['/']);
};
